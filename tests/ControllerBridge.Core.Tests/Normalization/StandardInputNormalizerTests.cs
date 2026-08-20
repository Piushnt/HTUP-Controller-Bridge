using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Normalization;
using Xunit;

namespace ControllerBridge.Core.Tests.Normalization;

public class StandardInputNormalizerTests
{
    private readonly StandardInputNormalizer _normalizer = new();

    [Fact]
    public void NormalizeAxis_WithCenteredValue_ReturnsZero()
    {
        // Arrange
        float rawValue = 0f;
        float min = -1f;
        float max = 1f;

        // Act
        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        // Assert
        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void NormalizeAxis_WithMaxValue_ReturnsOne()
    {
        // Arrange
        float rawValue = 1f;
        float min = -1f;
        float max = 1f;

        // Act
        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        // Assert
        Assert.Equal(1f, result, 2);
    }

    [Fact]
    public void NormalizeAxis_WithMinValue_ReturnsNegativeOne()
    {
        // Arrange
        float rawValue = -1f;
        float min = -1f;
        float max = 1f;

        // Act
        float result = _normalizer.NormalizeAxis(rawValue, min, max);

        // Assert
        Assert.Equal(-1f, result, 2);
    }

    [Fact]
    public void NormalizeAxis_WithDeadzone_ReturnZeroForSmallValues()
    {
        // Arrange
        float rawValue = 0.05f;
        float min = -1f;
        float max = 1f;
        float deadzone = 0.1f;

        // Act
        float result = _normalizer.NormalizeAxis(rawValue, min, max, deadzone);

        // Assert
        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void NormalizeTrigger_WithMinValue_ReturnsZero()
    {
        // Arrange
        float rawValue = 0f;
        float min = 0f;
        float max = 1f;

        // Act
        float result = _normalizer.NormalizeTrigger(rawValue, min, max);

        // Assert
        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void NormalizeTrigger_WithMaxValue_ReturnsOne()
    {
        // Arrange
        float rawValue = 1f;
        float min = 0f;
        float max = 1f;

        // Act
        float result = _normalizer.NormalizeTrigger(rawValue, min, max);

        // Assert
        Assert.Equal(1f, result, 2);
    }

    [Fact]
    public void ApplyDeadzone_WithValueBelowThreshold_ReturnsZero()
    {
        // Arrange
        float value = 0.05f;
        float threshold = 0.1f;

        // Act
        float result = _normalizer.ApplyDeadzone(value, threshold);

        // Assert
        Assert.Equal(0f, result, 2);
    }

    [Fact]
    public void ApplyDeadzone_WithValueAboveThreshold_ReturnsProperlySacledValue()
    {
        // Arrange
        float value = 0.5f;
        float threshold = 0.1f;

        // Act
        float result = _normalizer.ApplyDeadzone(value, threshold);

        // Assert
        Assert.True(result > 0);
        Assert.True(result <= 1f);
    }

    [Fact]
    public void ApplyResponseCurve_WithLinearCurve_ReturnsSameValue()
    {
        // Arrange
        float value = 0.75f;
        float curveExponent = 1f;

        // Act
        float result = _normalizer.ApplyResponseCurve(value, curveExponent);

        // Assert
        Assert.Equal(value, result, 2);
    }

    [Fact]
    public void ApplyResponseCurve_WithSquareCurve_ReturnsPoweredValue()
    {
        // Arrange
        float value = 0.5f;
        float curveExponent = 2f;

        // Act
        float result = _normalizer.ApplyResponseCurve(value, curveExponent);

        // Assert
        Assert.Equal(0.25f, result, 2);
    }
}
