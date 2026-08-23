using ControllerBridge.Core.Domain;
using ControllerBridge.Transport.Protocol;
using Xunit;

namespace ControllerBridge.Transport.Tests.Protocol;

public class ControllerMessageTests
{
    [Fact]
    public void Serialize_ProducesFixed64Bytes()
    {
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

        byte[] serialized = message.Serialize();

        Assert.Equal(64, serialized.Length);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
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
            DPad = 0x05, // X=0 (packed=1), Y=0 (packed=1) -> (1 | 4) = 5
            Flags = 0x01,
            ClientId = 12345
        };

        byte[] serialized = original.Serialize();
        var deserialized = ControllerMessage.Deserialize(serialized);

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
        Assert.Equal(original.ClientId, deserialized.ClientId);
    }

    [Fact]
    public void FromAndToControllerState_AccuratelyReconstructsDPad()
    {
        var state = new ControllerState
        {
            DPadX = 1,
            DPadY = -1,
            LeftStickX = new AnalogAxis(0.5f),
            LeftStickY = new AnalogAxis(-0.5f),
            LeftTrigger = 0.5f,
            RightTrigger = 1.0f,
            IsConnected = true
        };
        state.SetButtonState(ControllerButton.A, ButtonState.Pressed);
        state.SetButtonState(ControllerButton.RB, ButtonState.Pressed);

        var msg = ControllerMessage.FromControllerState(state);
        var reconstructed = msg.ToControllerState();

        Assert.Equal(1, reconstructed.DPadX);
        Assert.Equal(-1, reconstructed.DPadY);
        Assert.Equal(0.5f, reconstructed.LeftStickX.NormalizedValue, 2);
        Assert.Equal(-0.5f, reconstructed.LeftStickY.NormalizedValue, 2);
        Assert.Equal(ButtonState.Pressed, reconstructed.GetButtonState(ControllerButton.A));
        Assert.Equal(ButtonState.Pressed, reconstructed.GetButtonState(ControllerButton.RB));
        Assert.Equal(ButtonState.Released, reconstructed.GetButtonState(ControllerButton.B));
    }
}
