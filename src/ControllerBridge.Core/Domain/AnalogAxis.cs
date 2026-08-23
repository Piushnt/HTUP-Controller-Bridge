namespace ControllerBridge.Core.Domain;

/// <summary>
/// Represents an analog axis (joystick or trigger component) with raw and normalized values.
/// </summary>
public struct AnalogAxis
{
    /// <summary>
    /// Raw unnormalized value as read from the hardware.
    /// </summary>
    public float RawValue { get; set; }

    /// <summary>
    /// Normalized value in range [-1.0, 1.0] for sticks, or [0.0, 1.0] for triggers.
    /// </summary>
    public float NormalizedValue { get; set; }

    /// <summary>
    /// Timestamp of the last update in milliseconds.
    /// </summary>
    public ulong TimestampMs { get; set; }

    public AnalogAxis(float normalizedValue, float rawValue = 0f, ulong timestampMs = 0)
    {
        NormalizedValue = normalizedValue;
        RawValue = rawValue;
        TimestampMs = timestampMs;
    }

    public static implicit operator float(AnalogAxis axis) => axis.NormalizedValue;
    public static implicit operator AnalogAxis(float value) => new AnalogAxis(value);

    public override string ToString() => $"{NormalizedValue:F3} (raw: {RawValue:F1})";
}
