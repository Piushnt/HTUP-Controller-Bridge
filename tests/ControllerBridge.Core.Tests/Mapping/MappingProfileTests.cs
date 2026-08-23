using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Mapping;
using Xunit;

namespace ControllerBridge.Core.Tests.Mapping;

public class MappingProfileTests
{
    [Fact]
    public void CreateDefault_GeneratesIdentityMappings()
    {
        // Arrange
        string controllerId = "test-001";
        string controllerName = "Test Controller";

        // Act
        var profile = MappingProfile.CreateDefault(controllerId, controllerName);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(controllerId, profile.ControllerId);
        Assert.Equal(controllerName, profile.ControllerName);
        Assert.NotEmpty(profile.ButtonMappings);
    }

    [Fact]
    public void MapButton_WithoutMapping_ReturnsSourceButton()
    {
        // Arrange
        var profile = MappingProfile.CreateDefault("test-001", "Test");
        var sourceButton = ControllerButton.A;

        // Act
        var mappedButton = profile.MapButton(sourceButton);

        // Assert
        Assert.Equal(sourceButton, mappedButton);
    }

    [Fact]
    public void MapButton_WithCustomMapping_ReturnsTargetButton()
    {
        // Arrange
        var profile = new MappingProfile { ControllerId = "test-001" };
        profile.ButtonMappings.Add(new ButtonMapping
        {
            Source = ControllerButton.A,
            Target = ControllerButton.B,
            IsEnabled = true
        });

        // Act
        var mappedButton = profile.MapButton(ControllerButton.A);

        // Assert
        Assert.Equal(ControllerButton.B, mappedButton);
    }

    [Fact]
    public void MapButton_WithDisabledMapping_ReturnsSourceButton()
    {
        // Arrange
        var profile = new MappingProfile { ControllerId = "test-001" };
        profile.ButtonMappings.Add(new ButtonMapping
        {
            Source = ControllerButton.A,
            Target = ControllerButton.B,
            IsEnabled = false
        });

        // Act
        var mappedButton = profile.MapButton(ControllerButton.A);

        // Assert
        Assert.Equal(ControllerButton.A, mappedButton);
    }
}
