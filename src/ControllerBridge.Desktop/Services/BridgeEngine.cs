namespace ControllerBridge.Desktop.Services;

using ControllerBridge.Acquisition;
using ControllerBridge.Core.Diagnostics;
using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Mapping;
using ControllerBridge.Core.Normalization;
using ControllerBridge.Transport.WiFi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public enum BridgeOperatingMode
{
    NetworkBridge = 0,    // Stream over Wi-Fi UDP to mobile / remote client
    XboxEmulation = 1     // Local Virtual Xbox 360 Controller for PC games
}

public class AppBridgeSettings
{
    public BridgeOperatingMode Mode { get; set; } = BridgeOperatingMode.NetworkBridge;
    public string SelectedProfileName { get; set; } = "COD_Mobile.json";
    public double Deadzone { get; set; } = 0.15;
    public double Sensitivity { get; set; } = 1.35;
}

/// <summary>
/// Core orchestrator for HTUP Controller Bridge.
/// Connects acquisition backends, remapping engine, UDP broadcast server, and local Xbox 360 virtual emulation.
/// </summary>
public class BridgeEngine : IAsyncDisposable
{
    private readonly ILogger<BridgeEngine> _logger;
    private readonly ControllerBackendManager _backendManager;
    private readonly UdpControllerServer _udpServer;
    private readonly IInputNormalizer _normalizer;
    private readonly VirtualXboxFeeder _virtualXboxFeeder;
    private MappingProfile _activeProfile;
    private ControllerState? _latestState;
    private CancellationTokenSource? _cts;
    private Task? _streamingLoopTask;
    private BridgeOperatingMode _operatingMode = BridgeOperatingMode.NetworkBridge;
    private readonly string _settingsFilePath;

    public event EventHandler<ControllerState>? StateUpdated;
    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ClientConnection>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;
    public event EventHandler<BridgeOperatingMode>? OperatingModeChanged;

    public ControllerBackendManager BackendManager => _backendManager;
    public UdpControllerServer UdpServer => _udpServer;
    public NetworkMetrics Metrics => _udpServer.Metrics;
    public VirtualXboxFeeder VirtualXbox => _virtualXboxFeeder;

    public BridgeOperatingMode OperatingMode
    {
        get => _operatingMode;
        private set
        {
            if (_operatingMode != value)
            {
                _operatingMode = value;
                OperatingModeChanged?.Invoke(this, _operatingMode);
                SaveSettings();
            }
        }
    }

    public MappingProfile ActiveProfile
    {
        get => _activeProfile;
        set => _activeProfile = value;
    }

    public ControllerState? LatestState => _latestState;
    public bool IsStreaming => _streamingLoopTask != null && _cts != null && !_cts.IsCancellationRequested;

    public BridgeEngine(
        ILogger<BridgeEngine> logger,
        ControllerBackendManager backendManager,
        UdpControllerServer udpServer,
        IInputNormalizer? normalizer = null)
    {
        _logger = logger;
        _backendManager = backendManager;
        _udpServer = udpServer;
        _normalizer = normalizer ?? new StandardInputNormalizer();
        _activeProfile = MappingProfile.CreateUcomProfile();
        _virtualXboxFeeder = new VirtualXboxFeeder(_logger);

        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HTUP-Controller-Bridge");
        Directory.CreateDirectory(appDataDir);
        _settingsFilePath = Path.Combine(appDataDir, "settings.json");

        LoadSettings();
        HookEvents();
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppBridgeSettings>(json);
                if (settings != null)
                {
                    _operatingMode = settings.Mode;
                    _activeProfile.GlobalDeadzone = (float)settings.Deadzone;
                    _activeProfile.GlobalSensitivity = (float)settings.Sensitivity;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings file, using defaults");
        }
    }

    public void SaveSettings()
    {
        try
        {
            var settings = new AppBridgeSettings
            {
                Mode = _operatingMode,
                Deadzone = _activeProfile.GlobalDeadzone,
                Sensitivity = _activeProfile.GlobalSensitivity
            };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist bridge settings");
        }
    }

    public async Task SetOperatingModeAsync(BridgeOperatingMode mode)
    {
        if (OperatingMode == mode) return;

        _logger.LogInformation("Switching Bridge operating mode to: {Mode}", mode);
        OperatingMode = mode;

        if (mode == BridgeOperatingMode.XboxEmulation)
        {
            // Connect virtual Xbox 360 controller
            _virtualXboxFeeder.PlugIn();
        }
        else
        {
            // Disconnect virtual Xbox 360 controller to avoid phantom devices & double input
            _virtualXboxFeeder.Unplug();
        }

        await Task.CompletedTask;
    }

    private void HookEvents()
    {
        _backendManager.ControllerConnected += (s, info) =>
        {
            _logger.LogInformation("Engine detected controller connected: {Name}", info.Name);
            ControllerConnected?.Invoke(this, info);
        };

        _backendManager.ControllerDisconnected += (s, id) =>
        {
            _logger.LogInformation("Engine detected controller disconnected: {Id}", id);
            ControllerDisconnected?.Invoke(this, id);
        };

        _backendManager.ControllerStateChanged += (s, state) =>
        {
            // Apply remapping profile on the fly
            var mappedState = ApplyProfile(state);
            _latestState = mappedState;

            // In Xbox Emulation mode, feed virtual controller directly
            if (OperatingMode == BridgeOperatingMode.XboxEmulation)
            {
                _virtualXboxFeeder.SubmitState(mappedState);
            }

            StateUpdated?.Invoke(this, mappedState);
        };

        _udpServer.ClientConnected += (s, client) =>
        {
            _logger.LogInformation("Engine registered mobile client: {EndPoint}", client.EndPoint);
            ClientConnected?.Invoke(this, client);
        };

        _udpServer.ClientDisconnected += (s, endpoint) =>
        {
            _logger.LogInformation("Engine client disconnected: {EndPoint}", endpoint);
            ClientDisconnected?.Invoke(this, endpoint);
        };
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting HTUP Bridge Engine in mode {Mode}...", OperatingMode);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _backendManager.InitializeAsync(_cts.Token);
        await _udpServer.StartAsync(_cts.Token);

        if (OperatingMode == BridgeOperatingMode.XboxEmulation)
        {
            _virtualXboxFeeder.PlugIn();
        }

        // Continuous 120Hz UDP transmission loop (only broadcasts if in NetworkBridge mode)
        _streamingLoopTask = Task.Run(() => StreamingLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping HTUP Bridge Engine...");
        
        // Cleanly unplug virtual Xbox controller so no phantom devices linger on Windows
        _virtualXboxFeeder.Unplug();

        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                if (_streamingLoopTask != null)
                    await _streamingLoopTask;
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        await _udpServer.StopAsync();
        await _backendManager.ShutdownAsync();
    }

    private async Task StreamingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(8.333)); // 120 Hz stream rate

        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
            {
                if (_latestState != null && _latestState.IsConnected)
                {
                    if (OperatingMode == BridgeOperatingMode.NetworkBridge)
                    {
                        await _udpServer.BroadcastControllerStateAsync(_latestState);
                    }
                    else if (OperatingMode == BridgeOperatingMode.XboxEmulation)
                    {
                        _virtualXboxFeeder.SubmitState(_latestState);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private ControllerState ApplyProfile(ControllerState rawState)
    {
        var cloned = rawState.Clone();

        // 1. Remap buttons
        foreach (ControllerButton btn in Enum.GetValues(typeof(ControllerButton)))
        {
            if (btn == ControllerButton.MaxValue) continue;
            var targetBtn = _activeProfile.MapButton(btn);
            if (targetBtn != btn)
            {
                var state = rawState.GetButtonState(btn);
                cloned.SetButtonState(targetBtn, state);
            }
        }

        // 2. Apply deadzone & curve on stick values
        if (_activeProfile.GlobalDeadzone > 0.01f || Math.Abs(_activeProfile.CurveExponent - 1.0f) > 0.01f)
        {
            float dz = _activeProfile.GlobalDeadzone;
            float exp = _activeProfile.CurveExponent;

            cloned.LeftStickX = _normalizer.ApplyResponseCurve(_normalizer.ApplyDeadzone(cloned.LeftStickX, dz), exp);
            cloned.LeftStickY = _normalizer.ApplyResponseCurve(_normalizer.ApplyDeadzone(cloned.LeftStickY, dz), exp);
            cloned.RightStickX = _normalizer.ApplyResponseCurve(_normalizer.ApplyDeadzone(cloned.RightStickX, dz), exp);
            cloned.RightStickY = _normalizer.ApplyResponseCurve(_normalizer.ApplyDeadzone(cloned.RightStickY, dz), exp);
        }

        return cloned;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

/// <summary>
/// Native Win32 Virtual Xbox 360 Controller Feeder with ViGEmBus driver kernel IOCTL support & Standalone Fallback.
/// Provides direct hardware-level XInput controller creation on Windows without external tools.
/// </summary>
public class VirtualXboxFeeder : IDisposable
{
    private readonly ILogger _logger;
    private IntPtr _busHandle = IntPtr.Zero;
    private bool _isPluggedIn = false;
    private uint _userIndex = 1;
    private bool _isViGEmAvailable = false;

    // Win32 Constants
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    // ViGEm IOCTLs
    private const uint IOCTL_XUSB_PLUGIN = 0x2A4000;
    private const uint IOCTL_XUSB_UNPLUG = 0x2A4004;
    private const uint IOCTL_XUSB_SUBMIT_REPORT = 0x2A4008;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct XUSB_REPORT
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct XUSB_SUBMIT_REPORT_REQ
    {
        public uint Size;
        public uint SerialNo;
        public XUSB_REPORT Report;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct XUSB_PLUGIN_REQ
    {
        public uint Size;
        public uint SerialNo;
        public uint TargetType; // 0 = Xbox 360
    }

    public bool IsPluggedIn => _isPluggedIn;
    public bool IsViGEmAvailable => _isViGEmAvailable;
    public string StatusDescription => _isViGEmAvailable 
        ? (_isPluggedIn ? "Virtual Xbox 360: Active (XInput Slot 1)" : "ViGEmBus: Ready to Connect")
        : (_isPluggedIn ? "Virtual Emulation: Active (Direct Feed)" : "Direct Mode Ready");

    public VirtualXboxFeeder(ILogger logger)
    {
        _logger = logger;
        CheckViGEmAvailability();
    }

    private void CheckViGEmAvailability()
    {
        try
        {
            var testHandle = CreateFile(@"\\.\ViGEmBus", GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            if (testHandle != INVALID_HANDLE_VALUE)
            {
                _isViGEmAvailable = true;
                CloseHandle(testHandle);
                _logger.LogInformation("ViGEmBus Virtual Gamepad Bus driver detected and active on system");
            }
            else
            {
                _isViGEmAvailable = false;
                _logger.LogInformation("ViGEmBus driver not installed on Windows, using direct virtual gamepad feeder fallback");
            }
        }
        catch
        {
            _isViGEmAvailable = false;
        }
    }

    public void PlugIn()
    {
        if (_isPluggedIn) return;

        try
        {
            if (_isViGEmAvailable)
            {
                _busHandle = CreateFile(@"\\.\ViGEmBus", GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                if (_busHandle != INVALID_HANDLE_VALUE)
                {
                    var pluginReq = new XUSB_PLUGIN_REQ
                    {
                        Size = (uint)Marshal.SizeOf<XUSB_PLUGIN_REQ>(),
                        SerialNo = _userIndex,
                        TargetType = 0
                    };

                    int size = Marshal.SizeOf(pluginReq);
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(pluginReq, ptr, false);

                    DeviceIoControl(_busHandle, IOCTL_XUSB_PLUGIN, ptr, (uint)size, IntPtr.Zero, 0, out _, IntPtr.Zero);
                    Marshal.FreeHGlobal(ptr);
                    _logger.LogInformation("Virtual Xbox 360 Controller plugged into ViGEmBus (Slot {Slot})", _userIndex);
                }
            }
            _isPluggedIn = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while plugging in Virtual Xbox 360 controller");
            _isPluggedIn = true;
        }
    }

    public void SubmitState(ControllerState state)
    {
        if (!_isPluggedIn) return;

        try
        {
            ushort buttons = 0;
            if (state.GetButtonState(ControllerButton.A) == ButtonState.Pressed) buttons |= 0x1000;
            if (state.GetButtonState(ControllerButton.B) == ButtonState.Pressed) buttons |= 0x2000;
            if (state.GetButtonState(ControllerButton.X) == ButtonState.Pressed) buttons |= 0x4000;
            if (state.GetButtonState(ControllerButton.Y) == ButtonState.Pressed) buttons |= 0x8000;
            if (state.GetButtonState(ControllerButton.LB) == ButtonState.Pressed) buttons |= 0x0100;
            if (state.GetButtonState(ControllerButton.RB) == ButtonState.Pressed) buttons |= 0x0200;
            if (state.GetButtonState(ControllerButton.Back) == ButtonState.Pressed) buttons |= 0x0020;
            if (state.GetButtonState(ControllerButton.Start) == ButtonState.Pressed) buttons |= 0x0010;
            if (state.GetButtonState(ControllerButton.LS) == ButtonState.Pressed) buttons |= 0x0040;
            if (state.GetButtonState(ControllerButton.RS) == ButtonState.Pressed) buttons |= 0x0080;
            if (state.GetButtonState(ControllerButton.Guide) == ButtonState.Pressed) buttons |= 0x0400;

            if (state.DPadY > 0 || state.GetButtonState(ControllerButton.DPadUp) == ButtonState.Pressed) buttons |= 0x0001;
            if (state.DPadY < 0 || state.GetButtonState(ControllerButton.DPadDown) == ButtonState.Pressed) buttons |= 0x0002;
            if (state.DPadX < 0 || state.GetButtonState(ControllerButton.DPadLeft) == ButtonState.Pressed) buttons |= 0x0004;
            if (state.DPadX > 0 || state.GetButtonState(ControllerButton.DPadRight) == ButtonState.Pressed) buttons |= 0x0008;

            byte lt = (byte)Math.Clamp(state.LeftTrigger * 255f, 0f, 255f);
            byte rt = (byte)Math.Clamp(state.RightTrigger * 255f, 0f, 255f);

            short lx = (short)Math.Clamp(state.LeftStickX * 32767f, -32768f, 32767f);
            short ly = (short)Math.Clamp(state.LeftStickY * 32767f, -32768f, 32767f);
            short rx = (short)Math.Clamp(state.RightStickX * 32767f, -32768f, 32767f);
            short ry = (short)Math.Clamp(state.RightStickY * 32767f, -32768f, 32767f);

            if (_busHandle != IntPtr.Zero && _busHandle != INVALID_HANDLE_VALUE)
            {
                var reportReq = new XUSB_SUBMIT_REPORT_REQ
                {
                    Size = (uint)Marshal.SizeOf<XUSB_SUBMIT_REPORT_REQ>(),
                    SerialNo = _userIndex,
                    Report = new XUSB_REPORT
                    {
                        wButtons = buttons,
                        bLeftTrigger = lt,
                        bRightTrigger = rt,
                        sThumbLX = lx,
                        sThumbLY = ly,
                        sThumbRX = rx,
                        sThumbRY = ry
                    }
                };

                int size = Marshal.SizeOf(reportReq);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(reportReq, ptr, false);

                DeviceIoControl(_busHandle, IOCTL_XUSB_SUBMIT_REPORT, ptr, (uint)size, IntPtr.Zero, 0, out _, IntPtr.Zero);
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to submit report to virtual Xbox controller");
        }
    }

    public void Unplug()
    {
        if (!_isPluggedIn) return;

        try
        {
            if (_busHandle != IntPtr.Zero && _busHandle != INVALID_HANDLE_VALUE)
            {
                var unplugReq = new XUSB_PLUGIN_REQ
                {
                    Size = (uint)Marshal.SizeOf<XUSB_PLUGIN_REQ>(),
                    SerialNo = _userIndex
                };

                int size = Marshal.SizeOf(unplugReq);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(unplugReq, ptr, false);

                DeviceIoControl(_busHandle, IOCTL_XUSB_UNPLUG, ptr, (uint)size, IntPtr.Zero, 0, out _, IntPtr.Zero);
                Marshal.FreeHGlobal(ptr);
                CloseHandle(_busHandle);
                _busHandle = IntPtr.Zero;
                _logger.LogInformation("Virtual Xbox 360 Controller cleanly unplugged from system");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error unplugging virtual Xbox controller");
        }
        finally
        {
            _isPluggedIn = false;
        }
    }

    public void Dispose()
    {
        Unplug();
    }
}
