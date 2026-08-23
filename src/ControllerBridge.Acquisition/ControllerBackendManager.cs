namespace ControllerBridge.Acquisition;

using ControllerBridge.Acquisition.Platforms.Windows;
using ControllerBridge.Acquisition.Simulated;
using ControllerBridge.Core.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Unified Controller Backend Manager.
/// Automatically detects and prioritizes physical controllers (Windows Joystick/DirectInput),
/// and seamlessly falls back or switches to simulated input for testing.
/// </summary>
public class ControllerBackendManager : IControllerBackend
{
    private readonly ILogger<ControllerBackendManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private IControllerBackend _activeBackend;
    private readonly SimulatedControllerBackend _simulatedBackend;
    private readonly IControllerBackend? _nativeBackend;
    private bool _useSimulation;

    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ControllerState>? ControllerStateChanged;

    public bool IsSimulationActive => _useSimulation;

    public ControllerBackendManager(ILogger<ControllerBackendManager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

        _simulatedBackend = new SimulatedControllerBackend(_loggerFactory.CreateLogger<SimulatedControllerBackend>());

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _nativeBackend = new WindowsJoystickBackend(_loggerFactory.CreateLogger<WindowsJoystickBackend>());
            _activeBackend = _nativeBackend;
            _useSimulation = false;
        }
        else
        {
            _nativeBackend = null;
            _activeBackend = _simulatedBackend;
            _useSimulation = true;
        }

        HookEvents(_simulatedBackend);
        if (_nativeBackend != null)
        {
            HookEvents(_nativeBackend);
        }
    }

    private void HookEvents(IControllerBackend backend)
    {
        backend.ControllerConnected += (s, info) =>
        {
            if (s == _activeBackend)
                ControllerConnected?.Invoke(this, info);
        };
        backend.ControllerDisconnected += (s, id) =>
        {
            if (s == _activeBackend)
                ControllerDisconnected?.Invoke(this, id);
        };
        backend.ControllerStateChanged += (s, state) =>
        {
            if (s == _activeBackend)
                ControllerStateChanged?.Invoke(this, state);
        };
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing Controller Backend Manager (Mode: {Mode})", 
            _useSimulation ? "Simulated" : "Native Windows");

        if (_nativeBackend != null)
        {
            await _nativeBackend.InitializeAsync(ct);
            // If no physical controllers were found initially, we still keep native running for hot-plug
        }

        if (_useSimulation)
        {
            await _simulatedBackend.InitializeAsync(ct);
        }
    }

    public async Task SetSimulationModeAsync(bool enableSimulation)
    {
        if (_useSimulation == enableSimulation)
            return;

        _logger.LogInformation("Switching Controller Backend to: {Mode}", enableSimulation ? "Simulated" : "Native");

        // Disconnect old controllers
        var oldControllers = _activeBackend.GetConnectedControllers();
        foreach (var c in oldControllers)
        {
            ControllerDisconnected?.Invoke(this, c.Id);
        }

        if (enableSimulation)
        {
            _useSimulation = true;
            _activeBackend = _simulatedBackend;
            await _simulatedBackend.InitializeAsync();
        }
        else
        {
            await _simulatedBackend.ShutdownAsync();
            _useSimulation = false;
            _activeBackend = _nativeBackend ?? _simulatedBackend;
        }

        // Announce new controllers
        var newControllers = _activeBackend.GetConnectedControllers();
        foreach (var c in newControllers)
        {
            ControllerConnected?.Invoke(this, c);
        }
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down Controller Backend Manager");
        if (_nativeBackend != null)
            await _nativeBackend.ShutdownAsync(ct);
        await _simulatedBackend.ShutdownAsync(ct);
    }

    public List<ControllerInfo> GetConnectedControllers() => _activeBackend.GetConnectedControllers();

    public ControllerState? GetControllerState(string controllerId) => _activeBackend.GetControllerState(controllerId);

    public bool SupportsVibration => _activeBackend.SupportsVibration;

    /// <summary>
    /// Routes vibration to the active backend. Fully fault-tolerant.
    /// </summary>
    public Task SetVibrationAsync(string controllerId, float leftMotor, float rightMotor, ushort durationMs = 200)
        => _activeBackend.SetVibrationAsync(controllerId, leftMotor, rightMotor, durationMs);
}
