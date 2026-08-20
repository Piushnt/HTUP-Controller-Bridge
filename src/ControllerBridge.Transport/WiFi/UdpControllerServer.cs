namespace ControllerBridge.Transport.WiFi;

using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Diagnostics;
using ControllerBridge.Transport.Protocol;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// UDP server for streaming controller state to mobile clients.
/// </summary>
public class UdpControllerServer : IAsyncDisposable
{
    private readonly ILogger<UdpControllerServer> _logger;
    private readonly int _port;
    private UdpClient? _udpClient;
    private Dictionary<string, ClientConnection> _clients = new();
    private CancellationTokenSource? _cts;
    private Task? _broadcastTask;
    private uint _nextClientId = 1;

    public event EventHandler<ClientConnection>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;

    public NetworkMetrics Metrics { get; } = new();

    public UdpControllerServer(ILogger<UdpControllerServer> logger, int port = 5555)
    {
        _logger = logger;
        _port = port;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting UDP server on port {Port}", _port);
        
        _udpClient = new UdpClient(_port);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _broadcastTask = ReceiveClientsAsync(_cts.Token);
        
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping UDP server");
        
        if (_cts != null)
        {
            _cts.Cancel();
            if (_broadcastTask != null)
                await _broadcastTask;
            _cts.Dispose();
        }

        _udpClient?.Dispose();
    }

    public async Task BroadcastControllerStateAsync(ControllerState state)
    {
        if (_udpClient == null || _clients.Count == 0)
            return;

        var message = ControllerMessage.FromControllerState(state);
        byte[] buffer = message.Serialize();

        foreach (var client in _clients.Values.ToList())
        {
            try
            {
                await _udpClient.SendAsync(buffer, buffer.Length, client.EndPoint);
                Metrics.BytesSent += (ulong)buffer.Length;
                Metrics.TotalPacketsSent++;
                Metrics.LastPacketSent = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send to client {ClientId}", client.Id);
            }
        }

        Metrics.PacketsPerSecond = Metrics.TotalPacketsSent > 0 
            ? Metrics.TotalPacketsSent / DateTime.UtcNow.Subtract(Metrics.LastPacketSent).TotalSeconds 
            : 0;
    }

    public List<ClientConnection> GetConnectedClients()
    {
        return _clients.Values.ToList();
    }

    private async Task ReceiveClientsAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
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
                            ConnectedAt = DateTime.UtcNow
                        };

                        _clients[clientKey] = client;
                        _logger.LogInformation("Client connected: {EndPoint} (ID: {ClientId})", 
                            result.RemoteEndPoint, client.Id);
                        ClientConnected?.Invoke(this, client);
                    }

                    client.LastSeen = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Socket error in receive loop");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Receive loop cancelled");
        }
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
