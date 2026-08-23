namespace ControllerBridge.Transport.WiFi;

using ControllerBridge.Core.Diagnostics;
using ControllerBridge.Core.Domain;
using ControllerBridge.Transport.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// High-performance UDP server for streaming controller state to mobile clients at 120Hz.
/// </summary>
public class UdpControllerServer : IAsyncDisposable
{
    private readonly ILogger<UdpControllerServer> _logger;
    private readonly int _port;
    private UdpClient? _udpClient;
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _pruneTask;
    private uint _nextClientId = 1;
    private long _packetsInCurrentWindow;
    private DateTime _lastWindowReset = DateTime.UtcNow;

    public event EventHandler<ClientConnection>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;

    public NetworkMetrics Metrics { get; } = new();
    public int Port => _port;
    public bool IsRunning => _udpClient != null && _cts != null && !_cts.IsCancellationRequested;

    public UdpControllerServer(ILogger<UdpControllerServer> logger, int port = 5555)
    {
        _logger = logger;
        _port = port;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        _logger.LogInformation("Starting UDP server on port {Port}", _port);
        
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
        _udpClient.Client.ReceiveBufferSize = 65536;
        _udpClient.Client.SendBufferSize = 65536;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _receiveTask = Task.Run(() => ReceiveClientsLoopAsync(_cts.Token), _cts.Token);
        _pruneTask = Task.Run(() => PruneInactiveClientsLoopAsync(_cts.Token), _cts.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        _logger.LogInformation("Stopping UDP server");
        
        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                if (_receiveTask != null)
                    await _receiveTask;
                if (_pruneTask != null)
                    await _pruneTask;
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        _udpClient?.Dispose();
        _udpClient = null;
        _clients.Clear();
    }

    public async Task BroadcastControllerStateAsync(ControllerState state)
    {
        if (_udpClient == null || _clients.IsEmpty)
            return;

        var message = ControllerMessage.FromControllerState(state);
        byte[] buffer = message.Serialize();

        foreach (var client in _clients.Values)
        {
            try
            {
                await _udpClient.SendAsync(buffer, buffer.Length, client.EndPoint);
                Metrics.BytesSent += (ulong)buffer.Length;
                Metrics.TotalPacketsSent++;
                Metrics.LastPacketSent = DateTime.UtcNow;
                Interlocked.Increment(ref _packetsInCurrentWindow);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to send UDP packet to client {ClientId}", client.Id);
            }
        }

        UpdatePacketsPerSecond();
    }

    private void UpdatePacketsPerSecond()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastWindowReset).TotalSeconds;
        if (elapsed >= 1.0)
        {
            long packets = Interlocked.Exchange(ref _packetsInCurrentWindow, 0);
            Metrics.PacketsPerSecond = packets / elapsed;
            _lastWindowReset = now;
        }
    }

    public List<ClientConnection> GetConnectedClients() => _clients.Values.ToList();

    private async Task ReceiveClientsLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(ct);
                    Metrics.BytesReceived += (ulong)result.Buffer.Length;
                    Metrics.TotalPacketsReceived++;
                    Metrics.LastPacketReceived = DateTime.UtcNow;

                    string clientKey = result.RemoteEndPoint.ToString();

                    if (!_clients.TryGetValue(clientKey, out var client))
                    {
                        client = new ClientConnection
                        {
                            Id = _nextClientId++,
                            EndPoint = result.RemoteEndPoint,
                            ConnectedAt = DateTime.UtcNow,
                            LastSeen = DateTime.UtcNow
                        };

                        _clients[clientKey] = client;
                        _logger.LogInformation("New Mobile Client registered: {EndPoint} (ID: {ClientId})", 
                            result.RemoteEndPoint, client.Id);
                        ClientConnected?.Invoke(this, client);

                        // Send back an immediate handshake response
                        var ackMsg = new ControllerMessage
                        {
                            Type = MessageType.HandshakeResponse,
                            ClientId = client.Id,
                            SequenceNumber = 0,
                            TimestampMs = (ulong)Environment.TickCount64
                        };
                        byte[] ackBytes = ackMsg.Serialize();
                        await _udpClient.SendAsync(ackBytes, ackBytes.Length, client.EndPoint);
                    }
                    else
                    {
                        client.LastSeen = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        _logger.LogTrace(ex, "Socket exception in UDP receive loop");
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task PruneInactiveClientsLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(2000, ct);
                var now = DateTime.UtcNow;

                foreach (var kvp in _clients)
                {
                    if ((now - kvp.Value.LastSeen).TotalSeconds > 6.0)
                    {
                        if (_clients.TryRemove(kvp.Key, out var removed))
                        {
                            _logger.LogInformation("Mobile Client timed out / disconnected: {EndPoint} (ID: {ClientId})", 
                                removed.EndPoint, removed.Id);
                            ClientDisconnected?.Invoke(this, removed.EndPoint.ToString());
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

public class ClientConnection
{
    public uint Id { get; set; }
    public IPEndPoint EndPoint { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan ConnectionDuration => DateTime.UtcNow - ConnectedAt;
}
