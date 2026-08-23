namespace ControllerBridge.Core.Normalization;

using Domain;

/// <summary>
/// Type of deadzone processing.
/// </summary>
public enum DeadzoneType
{
    Axial,
    Radial
}

/// <summary>
/// Interface for input normalization backends.
/// Converts platform-specific raw input to canonical ControllerState.
/// </summary>
public interface IInputNormalizer
{
    /// <summary>
    /// Normalize raw single-axis value to [-1.0, 1.0] range.
    /// </summary>
    float NormalizeAxis(float rawValue, float min, float max, float deadzone = 0.1f);

    /// <summary>
    /// Normalize a 2D stick (X and Y simultaneously) using optional radial deadzone.
    /// </summary>
    (float X, float Y) NormalizeStick2D(float rawX, float rawY, float minX, float maxX, float minY, float maxY, float deadzone = 0.1f, DeadzoneType deadzoneType = DeadzoneType.Radial);

    /// <summary>
    /// Normalize trigger value to [0.0, 1.0] range.
    /// </summary>
    float NormalizeTrigger(float rawValue, float min, float max, float deadzone = 0.05f);

    /// <summary>
    /// Apply deadzone processing to a single axis value in [-1.0, 1.0].
    /// </summary>
    float ApplyDeadzone(float value, float threshold);

    /// <summary>
    /// Apply response curve to axis (for sensitivity adjustment: 1.0 = linear, 2.0 = quadratic/smooth).
    /// </summary>
    float ApplyResponseCurve(float value, float curveExponent);
}

/// <summary>
/// Standard input normalizer implementation with high-precision radial and axial algorithms.
/// </summary>
public class StandardInputNormalizer : IInputNormalizer
{
    public float NormalizeAxis(float rawValue, float min, float max, float deadzone = 0.1f)
    {
        if (Math.Abs(max - min) < float.Epsilon)
            return 0f;

        // Center around zero
        float midpoint = (min + max) / 2f;
        float range = (max - min) / 2f;
        float centered = (rawValue - midpoint) / range;

        // Clamp to [-1, 1]
        centered = Math.Clamp(centered, -1f, 1f);

        // Apply deadzone
        return ApplyDeadzone(centered, deadzone);
    }

    public (float X, float Y) NormalizeStick2D(float rawX, float rawY, float minX, float maxX, float minY, float maxY, float deadzone = 0.1f, DeadzoneType deadzoneType = DeadzoneType.Radial)
    {
        // 1. Center and normalize X & Y to [-1, 1]
        float midX = (minX + maxX) / 2f;
        float rangeX = (maxX - minX) / 2f;
        float nx = rangeX > float.Epsilon ? Math.Clamp((rawX - midX) / rangeX, -1f, 1f) : 0f;

        float midY = (minY + maxY) / 2f;
        float rangeY = (maxY - minY) / 2f;
        float ny = rangeY > float.Epsilon ? Math.Clamp((rawY - midY) / rangeY, -1f, 1f) : 0f;

        if (deadzoneType == DeadzoneType.Axial)
        {
            return (ApplyDeadzone(nx, deadzone), ApplyDeadzone(ny, deadzone));
        }

        // 2. Radial deadzone
        float magnitude = MathF.Sqrt(nx * nx + ny * ny);
        if (magnitude < deadzone)
        {
            return (0f, 0f);
        }

        // Rescale vector from deadzone to 1.0
        float normalizedMagnitude = Math.Clamp((magnitude - deadzone) / (1.0f - deadzone), 0f, 1f);
        float scale = normalizedMagnitude / magnitude;

        return (Math.Clamp(nx * scale, -1f, 1f), Math.Clamp(ny * scale, -1f, 1f));
    }

    public float NormalizeTrigger(float rawValue, float min, float max, float deadzone = 0.05f)
    {
        if (Math.Abs(max - min) < float.Epsilon)
            return 0f;

        // Normalize to [0, 1]
        float normalized = (rawValue - min) / (max - min);
        normalized = Math.Clamp(normalized, 0f, 1f);

        if (normalized < deadzone)
            return 0f;

        return Math.Clamp((normalized - deadzone) / (1f - deadzone), 0f, 1f);
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
        if (Math.Abs(curveExponent - 1f) < 0.001f || Math.Abs(value) < float.Epsilon)
            return value;

        float sign = Math.Sign(value);
        float absValue = Math.Abs(value);
        return sign * (float)Math.Pow(absValue, curveExponent);
    }
}
