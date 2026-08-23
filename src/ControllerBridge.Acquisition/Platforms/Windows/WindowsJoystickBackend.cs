namespace ControllerBridge.Acquisition.Platforms.Windows;

using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Normalization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Native Windows Joystick & DirectInput backend utilizing WinMM & HID.
/// Captures input from all USB gamepads including generic Ucom controllers with ultra-low latency.
/// </summary>
public class WindowsJoystickBackend : IControllerBackend
{
    #region WinMM Native Interop
    private const int JOY_RETURNX = 0x00000001;
    private const int JOY_RETURNY = 0x00000002;
    private const int JOY_RETURNZ = 0x00000004;
    private const int JOY_RETURNR = 0x00000008;
    private const int JOY_RETURNU = 0x00000010;
    private const int JOY_RETURNV = 0x00000020;
    private const int JOY_RETURNPOV = 0x00000040;
    private const int JOY_RETURNBUTTONS = 0x00000080;
    private const int JOY_RETURNRAWDATA = 0x00000100;
    private const int JOY_RETURNPOVCTS = 0x00000200;
    private const int JOY_RETURNCENTERED = 0x00000400;
    private const int JOY_USEDEADZONE = 0x00000800;
    private const int JOY_RETURNALL = (JOY_RETURNX | JOY_RETURNY | JOY_RETURNZ |
                                       JOY_RETURNR | JOY_RETURNU | JOY_RETURNV |
                                       JOY_RETURNPOV | JOY_RETURNBUTTONS);

    private const int JOYERR_NOERROR = 0;
    private const int JOYERR_PARMS = 165;
    private const int JOYERR_NOCANDO = 166;
    private const int JOYERR_UNPLUGGED = 167;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct JOYCAPS
    {
        public ushort wMid;
        public ushort wPid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint wXmin;
        public uint wXmax;
        public uint wYmin;
        public uint wYmax;
        public uint wZmin;
        public uint wZmax;
        public uint wNumButtons;
        public uint wPeriodMin;
        public uint wPeriodMax;
        public uint wRmin;
        public uint wRmax;
        public uint wUmin;
        public uint wUmax;
        public uint wVmin;
        public uint wVmax;
        public uint wCaps;
        public uint wMaxAxes;
        public uint wNumAxes;
        public uint wMaxButtons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szRegKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szOEMVxD;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOYINFOEX
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwXpos;
        public uint dwYpos;
        public uint dwZpos;
        public uint dwRpos;
        public uint dwUpos;
        public uint dwVpos;
        public uint dwButtons;
        public uint dwButtonNumber;
        public uint dwPOV;
        public uint dwReserved1;
        public uint dwReserved2;
    }

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint joyGetNumDevs();

    [DllImport("winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint joyGetDevCaps(nuint uJoyID, out JOYCAPS pjc, uint cbjc);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint joyGetPosEx(uint uJoyID, ref JOYINFOEX pji);
    #endregion

    private readonly ILogger<WindowsJoystickBackend> _logger;
    private readonly IInputNormalizer _normalizer;
    private readonly ConcurrentDictionary<int, ControllerDeviceInternal> _activeDevices = new();
    private CancellationTokenSource? _cts;
    private Task? _pollLoopTask;
    private Task? _hotPlugWatchTask;

    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ControllerState>? ControllerStateChanged;

    public WindowsJoystickBackend(ILogger<WindowsJoystickBackend> logger, IInputNormalizer? normalizer = null)
    {
        _logger = logger;
        _normalizer = normalizer ?? new StandardInputNormalizer();
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing Windows Native Joystick & DirectInput Backend");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Initial scan
        ScanDevices();

        // Start polling and hot-plug surveillance
        _pollLoopTask = Task.Run(() => PollingLoopAsync(_cts.Token), _cts.Token);
        _hotPlugWatchTask = Task.Run(() => HotPlugWatcherAsync(_cts.Token), _cts.Token);

        return Task.CompletedTask;
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down Windows Joystick Backend");
        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                if (_pollLoopTask != null)
                    await _pollLoopTask;
                if (_hotPlugWatchTask != null)
                    await _hotPlugWatchTask;
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cts.Dispose();
            }
        }
        _activeDevices.Clear();
    }

    public List<ControllerInfo> GetConnectedControllers()
    {
        var result = new List<ControllerInfo>();
        foreach (var dev in _activeDevices.Values)
        {
            result.Add(dev.Info);
        }
        return result;
    }

    public ControllerState? GetControllerState(string controllerId)
    {
        foreach (var dev in _activeDevices.Values)
        {
            if (dev.Info.Id == controllerId)
                return dev.LatestState.Clone();
        }
        return null;
    }

    private void ScanDevices()
    {
        uint maxDevs = joyGetNumDevs();
        for (uint i = 0; i < maxDevs && i < 16; i++)
        {
            var infoEx = new JOYINFOEX
            {
                dwSize = (uint)Marshal.SizeOf<JOYINFOEX>(),
                dwFlags = JOY_RETURNALL
            };

            uint result = joyGetPosEx(i, ref infoEx);
            if (result == JOYERR_NOERROR)
            {
                if (!_activeDevices.ContainsKey((int)i))
                {
                    // Query capabilities
                    JOYCAPS caps = new JOYCAPS();
                    joyGetDevCaps(i, out caps, (uint)Marshal.SizeOf<JOYCAPS>());

                    string name = string.IsNullOrWhiteSpace(caps.szPname) ? $"USB Gamepad #{i + 1}" : caps.szPname;
                    string id = $"win-joy-{i}-{caps.wMid:X4}:{caps.wPid:X4}";

                    var capabilities = new ControllerCapabilities
                    {
                        Name = name,
                        ButtonCount = (int)Math.Max(caps.wNumButtons, 12),
                        AxisCount = (int)Math.Max(caps.wNumAxes, 4),
                        HasLeftStick = true,
                        HasRightStick = caps.wNumAxes >= 4,
                        HasDPad = true,
                        HasLeftTrigger = true,
                        HasRightTrigger = true
                    };

                    var info = new ControllerInfo
                    {
                        Id = id,
                        Name = name,
                        VendorId = caps.wMid.ToString("X4"),
                        ProductId = caps.wPid.ToString("X4"),
                        Capabilities = capabilities
                    };

                    var dev = new ControllerDeviceInternal
                    {
                        Index = (int)i,
                        Info = info,
                        Caps = caps,
                        LatestState = new ControllerState
                        {
                            ControllerId = id,
                            ControllerName = name,
                            IsConnected = true,
                            Capabilities = capabilities
                        }
                    };

                    _activeDevices[(int)i] = dev;
                    _logger.LogInformation("Controller connected [Index {Index}]: {Name} (ID: {Id})", i, name, id);
                    ControllerConnected?.Invoke(this, info);
                }
            }
            else
            {
                if (_activeDevices.TryRemove((int)i, out var removedDev))
                {
                    _logger.LogInformation("Controller disconnected [Index {Index}]: {Name}", i, removedDev.Info.Name);
                    ControllerDisconnected?.Invoke(this, removedDev.Info.Id);
                }
            }
        }
    }

    private async Task HotPlugWatcherAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                ScanDevices();
                await Task.Delay(1000, ct); // Check for new hardware connections every 1s
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(8)); // ~125 Hz acquisition

        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
            {
                foreach (var kvp in _activeDevices)
                {
                    int index = kvp.Key;
                    var dev = kvp.Value;

                    var infoEx = new JOYINFOEX
                    {
                        dwSize = (uint)Marshal.SizeOf<JOYINFOEX>(),
                        dwFlags = JOY_RETURNALL
                    };

                    uint res = joyGetPosEx((uint)index, ref infoEx);
                    if (res == JOYERR_NOERROR)
                    {
                        UpdateDeviceState(dev, ref infoEx);
                        ControllerStateChanged?.Invoke(this, dev.LatestState);
                    }
                    else if (res == JOYERR_UNPLUGGED)
                    {
                        if (_activeDevices.TryRemove(index, out var unplugged))
                        {
                            _logger.LogInformation("Controller unplugged during poll: {Name}", unplugged.Info.Name);
                            ControllerDisconnected?.Invoke(this, unplugged.Info.Id);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void UpdateDeviceState(ControllerDeviceInternal dev, ref JOYINFOEX info)
    {
        var state = dev.LatestState;
        state.SequenceNumber++;
        state.TimestampMs = (ulong)Environment.TickCount64;
        state.IsConnected = true;

        var caps = dev.Caps;

        // 1. Left Stick (X, Y)
        float minX = caps.wXmin, maxX = caps.wXmax > caps.wXmin ? caps.wXmax : 65535f;
        float minY = caps.wYmin, maxY = caps.wYmax > caps.wYmin ? caps.wYmax : 65535f;
        var leftStick = _normalizer.NormalizeStick2D(info.dwXpos, info.dwYpos, minX, maxX, minY, maxY, deadzone: 0.12f);
        state.LeftStickX = new AnalogAxis(leftStick.X, info.dwXpos, state.TimestampMs);
        state.LeftStickY = new AnalogAxis(leftStick.Y, info.dwYpos, state.TimestampMs);

        // 2. Right Stick (Typically Z/R or U/V depending on Ucom vs standard gamepad driver)
        float minZ = caps.wZmin, maxZ = caps.wZmax > caps.wZmin ? caps.wZmax : 65535f;
        float minR = caps.wRmin, maxR = caps.wRmax > caps.wRmin ? caps.wRmax : 65535f;
        var rightStick = _normalizer.NormalizeStick2D(info.dwZpos, info.dwRpos, minZ, maxZ, minR, maxR, deadzone: 0.12f);
        state.RightStickX = new AnalogAxis(rightStick.X, info.dwZpos, state.TimestampMs);
        state.RightStickY = new AnalogAxis(rightStick.Y, info.dwRpos, state.TimestampMs);

        // 3. POV / D-Pad (Returned in hundredths of a degree, 0 = Up, 9000 = Right, 18000 = Down, 27000 = Left, 65535 = Centered)
        if (info.dwPOV == 65535 || info.dwPOV > 36000)
        {
            state.DPadX = 0;
            state.DPadY = 0;
            state.SetButtonState(ControllerButton.DPadUp, ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadDown, ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadLeft, ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadRight, ButtonState.Released);
        }
        else
        {
            uint pov = info.dwPOV;
            bool up = (pov >= 31500 || pov <= 4500);
            bool right = (pov >= 4500 && pov <= 13500);
            bool down = (pov >= 13500 && pov <= 22500);
            bool left = (pov >= 22500 && pov <= 31500);

            state.DPadX = right ? 1 : (left ? -1 : 0);
            state.DPadY = up ? 1 : (down ? -1 : 0);

            state.SetButtonState(ControllerButton.DPadUp, up ? ButtonState.Pressed : ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadRight, right ? ButtonState.Pressed : ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadDown, down ? ButtonState.Pressed : ButtonState.Released);
            state.SetButtonState(ControllerButton.DPadLeft, left ? ButtonState.Pressed : ButtonState.Released);
        }

        // 4. Buttons (Bitmask mapping)
        uint btns = info.dwButtons;

        // Typical Ucom mapping:
        // Bit 0 (1): A (or X), Bit 1 (2): B (or A), Bit 2 (3): X (or B), Bit 3 (4): Y
        // Bit 4 (5): LB, Bit 5 (6): RB, Bit 6 (7): LT, Bit 7 (8): RT
        // Bit 8 (9): Back/Select, Bit 9 (10): Start
        // Bit 10 (11): LS (L3), Bit 11 (12): RS (R3)
        state.SetButtonState(ControllerButton.A, (btns & 0x01) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.B, (btns & 0x02) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.X, (btns & 0x04) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.Y, (btns & 0x08) != 0 ? ButtonState.Pressed : ButtonState.Released);

        state.SetButtonState(ControllerButton.LB, (btns & 0x10) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.RB, (btns & 0x20) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.LT_Button, (btns & 0x40) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.RT_Button, (btns & 0x80) != 0 ? ButtonState.Pressed : ButtonState.Released);

        state.LeftTrigger = (btns & 0x40) != 0 ? 1.0f : 0.0f;
        state.RightTrigger = (btns & 0x80) != 0 ? 1.0f : 0.0f;

        state.SetButtonState(ControllerButton.Back, (btns & 0x100) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.Start, (btns & 0x200) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.LS, (btns & 0x400) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.RS, (btns & 0x800) != 0 ? ButtonState.Pressed : ButtonState.Released);
        state.SetButtonState(ControllerButton.Guide, (btns & 0x1000) != 0 ? ButtonState.Pressed : ButtonState.Released);
    }

    private class ControllerDeviceInternal
    {
        public int Index { get; set; }
        public ControllerInfo Info { get; set; } = null!;
        public JOYCAPS Caps { get; set; }
        public ControllerState LatestState { get; set; } = null!;
    }
}
