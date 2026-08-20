namespace ControllerBridge.Core.Diagnostics;

/// <summary>
/// Real-time network metrics and diagnostics.
/// </summary>
public class NetworkMetrics
{
    // Timing
    public DateTime LastPacketReceived { get; set; }
    public DateTime LastPacketSent { get; set; }
    public double RoundTripTimeMs { get; set; }

    // Frequency
    public double PacketsPerSecond { get; set; }
    public double TargetFrequencyHz { get; set; } = 120.0;

    // Packet loss
    public ulong TotalPacketsSent { get; set; }
    public ulong TotalPacketsReceived { get; set; }
    public ulong PacketsLost { get; set; }
    public double PacketLossPercent => TotalPacketsSent > 0 
        ? (PacketsLost / (double)TotalPacketsSent) * 100.0 
        : 0.0;

    // Jitter
    public double JitterMs { get; set; }
    private Queue<double> _recentRttSamples = new(10);

    // Bytes
    public ulong BytesSent { get; set; }
    public ulong BytesReceived { get; set; }

    /// <summary>
    /// Update RTT measurement and calculate jitter.
    /// </summary>
    public void UpdateRtt(double rttMs)
    {
        RoundTripTimeMs = rttMs;
        _recentRttSamples.Enqueue(rttMs);
        
        if (_recentRttSamples.Count > 10)
            _recentRttSamples.Dequeue();

        // Calculate jitter as standard deviation
        if (_recentRttSamples.Count > 1)
        {
            double avg = _recentRttSamples.Average();
            double variance = _recentRttSamples.Average(x => Math.Pow(x - avg, 2));
            JitterMs = Math.Sqrt(variance);
        }
    }

    /// <summary>
    /// Reset all metrics.
    /// </summary>
    public void Reset()
    {
        LastPacketReceived = DateTime.UtcNow;
        LastPacketSent = DateTime.UtcNow;
        RoundTripTimeMs = 0;
        PacketsPerSecond = 0;
        TotalPacketsSent = 0;
        TotalPacketsReceived = 0;
        PacketsLost = 0;
        JitterMs = 0;
        BytesSent = 0;
        BytesReceived = 0;
        _recentRttSamples.Clear();
    }

    /// <summary>
    /// Get readable summary.
    /// </summary>
    public override string ToString()
    {
        return $"Metrics: {PacketsPerSecond:F1} Hz | RTT: {RoundTripTimeMs:F2}ms | Jitter: {JitterMs:F2}ms | Loss: {PacketLossPercent:F2}%";
    }
}

/// <summary>
/// Controller-specific diagnostics.
/// </summary>
public class ControllerDiagnostics
{
    public string ControllerId { get; set; } = "";
    public string ControllerName { get; set; } = "";
    public bool IsConnected { get; set; }
    public DateTime ConnectionTime { get; set; }
    public DateTime LastInputTime { get; set; }
    public ulong InputsReceived { get; set; }

    // Button press count for diagnostics
    public Dictionary<string, ulong> ButtonPressCount { get; set; } = new();

    /// <summary>
    /// Time since last input received.
    /// </summary>
    public TimeSpan TimeSinceLastInput => DateTime.UtcNow - LastInputTime;

    /// <summary>
    /// Connection duration.
    /// </summary>
    public TimeSpan ConnectionDuration => DateTime.UtcNow - ConnectionTime;
}
