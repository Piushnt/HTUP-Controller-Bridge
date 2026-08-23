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
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Core orchestrator for HTUP Controller Bridge.
/// Connects acquisition backends, remapping engine, and UDP broadcast server.
/// </summary>
public class BridgeEngine : IAsyncDisposable
{
    private readonly ILogger<BridgeEngine> _logger;
    private readonly ControllerBackendManager _backendManager;
    private readonly UdpControllerServer _udpServer;
    private readonly IInputNormalizer _normalizer;
    private MappingProfile _activeProfile;
    private ControllerState? _latestState;
    private CancellationTokenSource? _cts;
    private Task? _streamingLoopTask;

    public event EventHandler<ControllerState>? StateUpdated;
    public event EventHandler<ControllerInfo>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;
    public event EventHandler<ClientConnection>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;

    public ControllerBackendManager BackendManager => _backendManager;
    public UdpControllerServer UdpServer => _udpServer;
    public NetworkMetrics Metrics => _udpServer.Metrics;
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

        HookEvents();
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
        _logger.LogInformation("Starting HTUP Bridge Engine...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _backendManager.InitializeAsync(_cts.Token);
        await _udpServer.StartAsync(_cts.Token);

        // Continuous 120Hz UDP transmission loop
        _streamingLoopTask = Task.Run(() => StreamingLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping HTUP Bridge Engine...");
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
                    await _udpServer.BroadcastControllerStateAsync(_latestState);
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
