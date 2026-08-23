namespace ControllerBridge.Acquisition.Simulated;

using ControllerBridge.Core.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Simulated controller backend for testing without physical hardware.
/// Generates synthetic input for development and testing purposes.
/// </summary>
public class SimulatedControllerBackend : IControllerBackend
{
    private readonly ILogger<SimulatedControllerBackend> _logger;
    private ControllerState? _simulatedController;
    private CancellationTokenSource? _cts;
    private Task? _updateTask;
    private uint _sequenceNumber;

    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ControllerState>? ControllerStateChanged;

    public SimulatedControllerBackend(ILogger<SimulatedControllerBackend> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing simulated controller backend");
        
        _simulatedController = new ControllerState
        {
            ControllerId = "sim-001",
            ControllerName = "Simulated Gamepad",
            IsConnected = true,
            Capabilities = new ControllerCapabilities
            {
                Name = "Simulated Gamepad",
                HasButton_A = true,
                HasButton_B = true,
                HasButton_X = true,
                HasButton_Y = true,
                HasButton_LB = true,
                HasButton_RB = true,
                HasButton_LS = true,
                HasButton_RS = true,
                HasButton_Start = true,
                HasButton_Back = true,
                HasButton_Guide = true,
                HasLeftStick = true,
                HasRightStick = true,
                HasLeftTrigger = true,
                HasRightTrigger = true,
                TriggersAreAnalog = true,
                HasDPad = true,
                DPadIsButton = false,
            }
        };

        ControllerConnected?.Invoke(this, new ControllerInfo
        {
            Id = _simulatedController.ControllerId,
            Name = _simulatedController.ControllerName,
            Capabilities = _simulatedController.Capabilities
        });

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _updateTask = SimulationLoopAsync(_cts.Token);
        
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down simulated controller backend");
        
        if (_simulatedController != null)
        {
            ControllerDisconnected?.Invoke(this, _simulatedController.ControllerId);
        }

        if (_cts != null)
        {
            _cts.Cancel();
            if (_updateTask != null)
                await _updateTask;
            _cts.Dispose();
        }
    }

    public List<ControllerInfo> GetConnectedControllers()
    {
        if (_simulatedController == null)
            return new List<ControllerInfo>();

        return new List<ControllerInfo>
        {
            new ControllerInfo
            {
                Id = _simulatedController.ControllerId,
                Name = _simulatedController.ControllerName,
                Capabilities = _simulatedController.Capabilities
            }
        };
    }

    public ControllerState? GetControllerState(string controllerId)
    {
        if (_simulatedController?.ControllerId == controllerId)
            return _simulatedController.Clone();
        return null;
    }

    private async Task SimulationLoopAsync(CancellationToken ct)
    {
        var random = new Random();
        double time = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                time += 0.016; // ~60Hz simulation

                if (_simulatedController != null)
                {
                    _simulatedController.SequenceNumber = _sequenceNumber++;
                    _simulatedController.TimestampMs = (ulong)(time * 1000);

                    // Simulate circular stick motion
                    _simulatedController.LeftStickX = (float)Math.Sin(time) * 0.7f;
                    _simulatedController.LeftStickY = (float)Math.Cos(time) * 0.7f;

                    // Simulate right stick slower rotation
                    _simulatedController.RightStickX = (float)Math.Sin(time * 0.5f) * 0.5f;
                    _simulatedController.RightStickY = (float)Math.Cos(time * 0.5f) * 0.5f;

                    // Simulate trigger oscillation
                    _simulatedController.LeftTrigger = (float)((Math.Sin(time * 2) + 1) / 2);
                    _simulatedController.RightTrigger = (float)((Math.Cos(time * 2) + 1) / 2);

                    // Randomly press buttons for testing
                    if (random.NextDouble() < 0.02) // 2% chance per frame
                    {
                        var buttons = (ControllerButton[])Enum.GetValues(typeof(ControllerButton));
                        var randomButton = buttons[random.Next(buttons.Length - 1)];
                        _simulatedController.SetButtonState(randomButton, ButtonState.Pressed);
                    }

                    // Release random buttons
                    foreach (var button in _simulatedController.GetPressedButtons())
                    {
                        if (random.NextDouble() < 0.05) // 5% chance to release
                            _simulatedController.SetButtonState(button, ButtonState.Released);
                    }

                    ControllerStateChanged?.Invoke(this, _simulatedController);
                }

                await Task.Delay(16, ct); // ~60Hz
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Simulation loop cancelled");
        }
    }
}
