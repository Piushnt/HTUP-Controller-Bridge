using ControllerBridge.Core.Domain;
using ControllerBridge.Transport.Protocol;
using Xunit;

namespace ControllerBridge.Transport.Tests.Protocol;

public class ControllerMessageTests
{
    [Fact]
    public void Serialize_ProducesFixedSize()
    {
        // Arrange
        var message = new ControllerMessage
        {
            ProtocolVersion = ProtocolVersion.Major,
            Type = MessageType.ControllerState,
            SequenceNumber = 1,
            TimestampMs = 1000,
            LeftStickX = 0.5f,
            LeftStickY = 0.5f,
            RightStickX = -0.5f,
            RightStickY = -0.5f,
            LeftTrigger = 128,
            RightTrigger = 255,
            Flags = 0x01
        };

        // Act
        byte[] serialized = message.Serialize();

        // Assert
        Assert.Equal(64, serialized.Length);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new ControllerMessage
        {
            ProtocolVersion = ProtocolVersion.Major,
            Type = MessageType.ControllerState,
            SequenceNumber = 42,
            TimestampMs = 5000,
            LeftStickX = 0.75f,
            LeftStickY = -0.25f,
            RightStickX = 0.0f,
            RightStickY = 1.0f,
            LeftTrigger = 100,
            RightTrigger = 200,
            DPad = 0x05,
            Flags = 0x01,
            ClientId = 12345
        };

        // Act
        byte[] serialized = original.Serialize();
        var deserialized = ControllerMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.SequenceNumber, deserialized.SequenceNumber);
        Assert.Equal(original.TimestampMs, deserialized.TimestampMs);
        Assert.Equal(original.LeftStickX, deserialized.LeftStickX, 3);
        Assert.Equal(original.LeftStickY, deserialized.LeftStickY, 3);
        Assert.Equal(original.RightStickX, deserialized.RightStickX, 3);
        Assert.Equal(original.RightStickY, deserialized.RightStickY, 3);
        Assert.Equal(original.LeftTrigger, deserialized.LeftTrigger);
        Assert.Equal(original.RightTrigger, deserialized.RightTrigger);
        Assert.Equal(original.DPad, deserialized.DPad);
        Assert.Equal(original.Flags, deserialized.Flags);
    }

    [Fact]
    public void FromControllerState_CreatesValidMessage()
    {
        // Arrange
        var state = new ControllerState
        {
            ControllerId = "test-001",
            ControllerName = "Test Controller",
            SequenceNumber = 10,
            TimestampMs = 2000,
            IsConnected = true,
            LeftStickX = new AnalogAxis { NormalizedValue = 0.5f },
            LeftStickY = new AnalogAxis { NormalizedValue = -0.5f },
            RightStickX = new AnalogAxis { NormalizedValue = 0.0f },
            RightStickY = new AnalogAxis { NormalizedValue = 1.0f },
            LeftTrigger = 0.4f,
            RightTrigger = 0.8f,
            DPadX = 1,
            DPadY = 0
        };

        // Act
        var message = ControllerMessage.FromControllerState(state);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(10u, message.SequenceNumber);
        Assert.Equal(2000ul, message.TimestampMs);
        Assert.Equal(0.5f, message.LeftStickX, 2);
        Assert.Equal(-0.5f, message.LeftStickY, 2);
        Assert.Equal((byte)(0.4f * 255), message.LeftTrigger);
        Assert.Equal((byte)(0.8f * 255), message.RightTrigger);
    }

    [Fact]
    public void ToControllerState_ReconstructsState()
    {
        // Arrange
        var message = new ControllerMessage
        {
            SequenceNumber = 5,
            TimestampMs = 1500,
            LeftStickX = 0.25f,
            LeftStickY = 0.75f,
            RightStickX = -0.5f,
            RightStickY = -0.75f,
            LeftTrigger = 64,
            RightTrigger = 192,
            Flags = 0x01
        };

        // Act
        var state = message.ToControllerState("test-001", "Test Controller");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("test-001", state.ControllerId);
        Assert.Equal("Test Controller", state.ControllerName);
        Assert.Equal(5u, state.SequenceNumber);
        Assert.Equal(1500ul, state.TimestampMs);
        Assert.True(state.IsConnected);
    }

    [Fact]
    public void ProtocolVersion_HasValidFormat()
    {
        // Assert
        Assert.True(ProtocolVersion.Major >= 0);
        Assert.True(ProtocolVersion.Minor >= 0);
        Assert.True(ProtocolVersion.Patch >= 0);
        Assert.Equal($"{ProtocolVersion.Major}.{ProtocolVersion.Minor}.{ProtocolVersion.Patch}", 
            ProtocolVersion.Version);
    }
}
