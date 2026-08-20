namespace ControllerBridge.Core.Normalization;

using Domain;

/// <summary>
/// Interface for input normalization backends.
/// Converts platform-specific raw input to canonical ControllerState.
/// </summary>
public interface IInputNormalizer
{
    /// <summary>
    /// Normalize raw axis value to [-1.0, 1.0] range.
    /// </summary>
    float NormalizeAxis(float rawValue, float min, float max, float deadzone = 0.1f);

    /// <summary>
    /// Normalize trigger value to [0.0, 1.0] range.
    /// </summary>
    float NormalizeTrigger(float rawValue, float min, float max);

    /// <summary>
    /// Apply deadzone processing to axis.
    /// </summary>
    float ApplyDeadzone(float value, float threshold);

    /// <summary>
    /// Apply response curve to axis (for sensitivity adjustment).
    /// </summary>
    float ApplyResponseCurve(float value, float curveExponent);
}

/// <summary>
/// Standard input normalizer implementation.
/// </summary>
public class StandardInputNormalizer : IInputNormalizer
{
    public float NormalizeAxis(float rawValue, float min, float max, float deadzone = 0.1f)
    {
        // Center around zero
        float midpoint = (min + max) / 2f;
        float range = (max - min) / 2f;
        float centered = (rawValue - midpoint) / range;

        // Clamp to [-1, 1]
        centered = Math.Clamp(centered, -1f, 1f);

        // Apply deadzone
        return ApplyDeadzone(centered, deadzone);
    }

    public float NormalizeTrigger(float rawValue, float min, float max)
    {
        // Normalize to [0, 1]
        float normalized = (rawValue - min) / (max - min);
        return Math.Clamp(normalized, 0f, 1f);
    }

    public float ApplyDeadzone(float value, float threshold)
    {
        if (Math.Abs(value) < threshold)
            return 0f;

        // Scale to remove deadzone gap
        float sign = Math.Sign(value);
        float absValue = Math.Abs(value);
        float scaled = (absValue - threshold) / (1f - threshold);
        
        return sign * Math.Clamp(scaled, 0f, 1f);
    }

    public float ApplyResponseCurve(float value, float curveExponent)
    {
        if (curveExponent == 1f)
            return value;

        float sign = Math.Sign(value);
        float absValue = Math.Abs(value);
        return sign * (float)Math.Pow(absValue, curveExponent);
    }
}
