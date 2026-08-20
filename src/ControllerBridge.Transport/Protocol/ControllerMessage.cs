namespace ControllerBridge.Transport.Protocol;

/// <summary>
/// Protocol version information.
/// </summary>
public static class ProtocolVersion
{
    public const byte Major = 1;
    public const byte Minor = 0;
    public const byte Patch = 0;

    public static string Version => $"{Major}.{Minor}.{Patch}";

    public static byte[] GetVersionBytes() => new[] { Major, Minor, Patch };
}

/// <summary>
/// Message types in the protocol.
/// </summary>
public enum MessageType : byte
{
    HandshakeRequest = 0x01,
    HandshakeResponse = 0x02,
    ControllerState = 0x03,
    Heartbeat = 0x04,
    Acknowledge = 0x05,
    Disconnect = 0x06,
    ConfigurationRequest = 0x07,
    ConfigurationResponse = 0x08,
    Diagnostic = 0x09
}

/// <summary>
/// Compact binary message format for controller state transmission.
/// Total size: ~64 bytes (designed to fit in a single UDP packet easily).
/// </summary>
public class ControllerMessage
{
    // Header (6 bytes)
    public byte ProtocolVersion { get; set; } = ProtocolVersion.Major;
    public MessageType Type { get; set; } = MessageType.ControllerState;
    public uint SequenceNumber { get; set; }

    // Timestamp (8 bytes)
    public ulong TimestampMs { get; set; }

    // Button state (3 bytes = 24 bits for 22 buttons)
    public byte[] ButtonBitfield { get; set; } = new byte[3];

    // Analog axes (16 bytes = 4 x float16)
    public float LeftStickX { get; set; }
    public float LeftStickY { get; set; }
    public float RightStickX { get; set; }
    public float RightStickY { get; set; }

    // Triggers (2 bytes = 2 x byte)
    public byte LeftTrigger { get; set; }  // [0-255] maps to [0.0-1.0]
    public byte RightTrigger { get; set; }

    // D-Pad (1 byte packed)
    public byte DPad { get; set; } // bits 0-1: X (-1,0,1), bits 2-3: Y (-1,0,1)

    // Connection state and flags (1 byte)
    public byte Flags { get; set; }

    // Client ID for multi-client support (4 bytes)
    public uint ClientId { get; set; }

    /// <summary>
    /// Serialize to bytes. Returns byte array ready for UDP transmission.
    /// </summary>
    public byte[] Serialize()
    {
        byte[] buffer = new byte[64];
        int offset = 0;

        // Header
        buffer[offset++] = ProtocolVersion;
        buffer[offset++] = (byte)Type;
        Array.Copy(BitConverter.GetBytes(SequenceNumber), 0, buffer, offset, 4);
        offset += 4;

        // Timestamp
        Array.Copy(BitConverter.GetBytes(TimestampMs), 0, buffer, offset, 8);
        offset += 8;

        // Buttons
        Array.Copy(ButtonBitfield, 0, buffer, offset, 3);
        offset += 3;

        // Analog axes (using float)
        Array.Copy(BitConverter.GetBytes(LeftStickX), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(LeftStickY), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(RightStickX), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(RightStickY), 0, buffer, offset, 4);
        offset += 4;

        // Triggers
        buffer[offset++] = LeftTrigger;
        buffer[offset++] = RightTrigger;

        // D-Pad
        buffer[offset++] = DPad;

        // Flags
        buffer[offset++] = Flags;

        // Client ID
        Array.Copy(BitConverter.GetBytes(ClientId), 0, buffer, offset, 4);
        offset += 4;

        return buffer;
    }

    /// <summary>
    /// Deserialize from bytes.
    /// </summary>
    public static ControllerMessage Deserialize(byte[] buffer)
    {
        if (buffer.Length < 42)
            throw new InvalidOperationException("Buffer too small for ControllerMessage");

        var msg = new ControllerMessage();
        int offset = 0;

        msg.ProtocolVersion = buffer[offset++];
        msg.Type = (MessageType)buffer[offset++];
        msg.SequenceNumber = BitConverter.ToUInt32(buffer, offset);
        offset += 4;

        msg.TimestampMs = BitConverter.ToUInt64(buffer, offset);
        offset += 8;

        Array.Copy(buffer, offset, msg.ButtonBitfield, 0, 3);
        offset += 3;

        msg.LeftStickX = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        msg.LeftStickY = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        msg.RightStickX = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        msg.RightStickY = BitConverter.ToSingle(buffer, offset);
        offset += 4;

        msg.LeftTrigger = buffer[offset++];
        msg.RightTrigger = buffer[offset++];
        msg.DPad = buffer[offset++];
        msg.Flags = buffer[offset++];

        if (buffer.Length >= offset + 4)
            msg.ClientId = BitConverter.ToUInt32(buffer, offset);

        return msg;
    }

    /// <summary>
    /// Convert from canonical ControllerState.
    /// </summary>
    public static ControllerMessage FromControllerState(Core.Domain.ControllerState state)
    {
        var msg = new ControllerMessage
        {
            SequenceNumber = state.SequenceNumber,
            TimestampMs = state.TimestampMs,
            LeftStickX = state.LeftStickX.NormalizedValue,
            LeftStickY = state.LeftStickY.NormalizedValue,
            RightStickX = state.RightStickX.NormalizedValue,
            RightStickY = state.RightStickY.NormalizedValue,
            LeftTrigger = (byte)(state.LeftTrigger * 255),
            RightTrigger = (byte)(state.RightTrigger * 255),
            Flags = (byte)(state.IsConnected ? 0x01 : 0x00)
        };

        // Pack D-Pad
        msg.DPad = (byte)((state.DPadX & 0x3) | ((state.DPadY & 0x3) << 2));

        // Pack buttons
        var buttons = state.GetPressedButtons();
        foreach (var button in buttons)
        {
            int bitIndex = (int)button;
            if (bitIndex < 24)
            {
                int byteIndex = bitIndex / 8;
                int bitOffset = bitIndex % 8;
                msg.ButtonBitfield[byteIndex] |= (byte)(1 << bitOffset);
            }
        }

        return msg;
    }

    /// <summary>
    /// Convert to canonical ControllerState.
    /// </summary>
    public Core.Domain.ControllerState ToControllerState(string controllerId, string controllerName)
    {
        var state = new Core.Domain.ControllerState
        {
            ControllerId = controllerId,
            ControllerName = controllerName,
            SequenceNumber = SequenceNumber,
            TimestampMs = TimestampMs,
            IsConnected = (Flags & 0x01) != 0
        };

        state.LeftStickX.NormalizedValue = LeftStickX;
        state.LeftStickY.NormalizedValue = LeftStickY;
        state.RightStickX.NormalizedValue = RightStickX;
        state.RightStickY.NormalizedValue = RightStickY;
        state.LeftTrigger = LeftTrigger / 255f;
        state.RightTrigger = RightTrigger / 255f;

        // Unpack D-Pad
        state.DPadX = (sbyte)(DPad & 0x3) - 1;
        state.DPadY = (sbyte)((DPad >> 2) & 0x3) - 1;

        // Unpack buttons
        for (int bit = 0; bit < 24; bit++)
        {
            int byteIndex = bit / 8;
            int bitOffset = bit % 8;
            if ((ButtonBitfield[byteIndex] & (1 << bitOffset)) != 0)
            {
                if (bit < (int)Core.Domain.ControllerButton.MaxValue)
                {
                    var button = (Core.Domain.ControllerButton)bit;
                    state.SetButtonState(button, Core.Domain.ButtonState.Pressed);
                }
            }
        }

        return state;
    }
}
