using ControllerBridge.Core.Normalization;
using Xunit;

namespace ControllerBridge.Core.Tests.Normalization;

public class StandardInputNormalizerTests
{
    private readonly StandardInputNormalizer _normalizer = new();

    [Fact]
    public void NormalizeAxis_WithCenteredValue_ReturnsZero()
    {
        float rawValue = 32767.5f;
        float min = 0f;
        float max = 65535f;

        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void NormalizeAxis_WithMaxValue_ReturnsOne()
    {
        float rawValue = 65535f;
        float min = 0f;
        float max = 65535f;

        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        Assert.Equal(1f, result, 2);
    }

    [Fact]
    public void NormalizeAxis_WithMinValue_ReturnsNegativeOne()
    {
        float rawValue = 0f;
        float min = 0f;
        float max = 65535f;

        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        Assert.Equal(-1f, result, 2);
    }

    [Fact]
    public void NormalizeStick2D_RadialDeadzone_FiltersSmallNoise()
    {
        // Noise within deadzone threshold (< 0.15)
        float rawX = 32767.5f + 500f;
        float rawY = 32767.5f + 500f;

        var (x, y) = _normalizer.NormalizeStick2D(rawX, rawY, 0f, 65535f, 0f, 65535f, deadzone: 0.15f);

        Assert.Equal(0f, x);
        Assert.Equal(0f, y);
    }

    [Fact]
    public void NormalizeStick2D_FullDeflection_ReachesOne()
    {
        float rawX = 65535f;
        float rawY = 32767.5f;

        var (x, y) = _normalizer.NormalizeStick2D(rawX, rawY, 0f, 65535f, 0f, 65535f, deadzone: 0.10f);

        Assert.Equal(1f, x, 2);
        Assert.Equal(0f, y, 2);
    }

    [Fact]
    public void NormalizeTrigger_WithMinValue_ReturnsZero()
    {
        float rawValue = 0f;
        float min = 0f;
        float max = 255f;

        float result = _normalizer.NormalizeTrigger(rawValue, min, max);

        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void NormalizeTrigger_WithMaxValue_ReturnsOne()
    {
        float rawValue = 255f;
        float min = 0f;
        float max = 255f;

        float result = _normalizer.NormalizeTrigger(rawValue, min, max);

        Assert.Equal(1f, result, 2);
    }
}
