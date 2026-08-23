namespace ControllerBridge.Transport.Protocol;

using System;
using System.Net;
using System.Text;

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
    Diagnostic = 0x09,
    DiscoveryRequest = 0x0A,
    DiscoveryResponse = 0x0B,
    Rumble = 0x0C
}

/// <summary>
/// Compact 64-byte binary message format for high-speed UDP transmission.
/// </summary>
public class ControllerMessage
{
    public const int PacketSize = 64;

    // Header (6 bytes)
    public byte ProtocolVersion { get; set; } = ControllerBridge.Transport.Protocol.ProtocolVersion.Major;
    public MessageType Type { get; set; } = MessageType.ControllerState;
    public uint SequenceNumber { get; set; }

    // Timestamp (8 bytes)
    public ulong TimestampMs { get; set; }

    // Button state (3 bytes = 24 bits)
    public byte[] ButtonBitfield { get; set; } = new byte[3];

    // Analog axes (16 bytes = 4 x float32 IEEE 754)
    public float LeftStickX { get; set; }
    public float LeftStickY { get; set; }
    public float RightStickX { get; set; }
    public float RightStickY { get; set; }

    // Triggers (2 bytes: [0-255] maps to [0.0-1.0])
    public byte LeftTrigger { get; set; }
    public byte RightTrigger { get; set; }

    // D-Pad (1 byte: bits 0-1 for X (0=-1, 1=0, 2=1), bits 2-3 for Y (0=-1, 1=0, 2=1))
    public byte DPad { get; set; }

    // Flags (1 byte: bit 0 = isConnected, bit 1 = isCalibrated)
    public byte Flags { get; set; }

    // Client ID (4 bytes)
    public uint ClientId { get; set; }

    /// <summary>
    /// Serializes message to a fixed 64-byte array with trailing checksum.
    /// </summary>
    public byte[] Serialize()
    {
        byte[] buffer = new byte[PacketSize];
        int offset = 0;

        // Header
        buffer[offset++] = ProtocolVersion;
        buffer[offset++] = (byte)Type;
        Array.Copy(BitConverter.GetBytes(SequenceNumber), 0, buffer, offset, 4);
        offset += 4;

        // Timestamp
        Array.Copy(BitConverter.GetBytes(TimestampMs), 0, buffer, offset, 8);
        offset += 8;

        // Buttons (3 bytes)
        Array.Copy(ButtonBitfield, 0, buffer, offset, 3);
        offset += 3;

        // Analog axes (4 x 4 bytes)
        Array.Copy(BitConverter.GetBytes(LeftStickX), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(LeftStickY), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(RightStickX), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(RightStickY), 0, buffer, offset, 4);
        offset += 4;

        // Triggers (2 bytes)
        buffer[offset++] = LeftTrigger;
        buffer[offset++] = RightTrigger;

        // D-Pad (1 byte)
        buffer[offset++] = DPad;

        // Flags (1 byte)
        buffer[offset++] = Flags;

        // Client ID (4 bytes)
        Array.Copy(BitConverter.GetBytes(ClientId), 0, buffer, offset, 4);
        offset += 4;

        // Calculate simple XOR checksum on offset 63 for frame integrity
        byte checksum = 0;
        for (int i = 0; i < PacketSize - 1; i++)
        {
            checksum ^= buffer[i];
        }
        buffer[PacketSize - 1] = checksum;

        return buffer;
    }

    /// <summary>
    /// Deserializes message from incoming bytes.
    /// </summary>
    public static ControllerMessage Deserialize(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 41)
            throw new ArgumentException($"Buffer too small for ControllerMessage ({buffer?.Length ?? 0} bytes)", nameof(buffer));

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
        {
            msg.ClientId = BitConverter.ToUInt32(buffer, offset);
        }

        return msg;
    }

    /// <summary>
    /// Converts domain ControllerState to network ControllerMessage.
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
            LeftTrigger = (byte)Math.Clamp(state.LeftTrigger * 255f, 0f, 255f),
            RightTrigger = (byte)Math.Clamp(state.RightTrigger * 255f, 0f, 255f),
            Flags = (byte)(state.IsConnected ? 0x01 : 0x00)
        };

        // Pack D-Pad: DPadX and DPadY are in [-1, 0, 1], add 1 to map into [0, 1, 2] (fits in 2 bits)
        int packedX = Math.Clamp(state.DPadX + 1, 0, 2);
        int packedY = Math.Clamp(state.DPadY + 1, 0, 2);
        msg.DPad = (byte)((packedX & 0x03) | ((packedY & 0x03) << 2));

        // Pack buttons bitfield
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
    /// Converts network ControllerMessage to domain ControllerState.
    /// </summary>
    public Core.Domain.ControllerState ToControllerState(string controllerId = "", string controllerName = "")
    {
        var state = new Core.Domain.ControllerState
        {
            ControllerId = controllerId,
            ControllerName = controllerName,
            SequenceNumber = SequenceNumber,
            TimestampMs = TimestampMs,
            IsConnected = (Flags & 0x01) != 0,
            LeftStickX = LeftStickX,
            LeftStickY = LeftStickY,
            RightStickX = RightStickX,
            RightStickY = RightStickY,
            LeftTrigger = LeftTrigger / 255.0f,
            RightTrigger = RightTrigger / 255.0f
        };

        // Unpack D-Pad: [0, 1, 2] - 1 maps back into [-1, 0, 1]
        state.DPadX = (int)(DPad & 0x03) - 1;
        state.DPadY = (int)((DPad >> 2) & 0x03) - 1;

        // Unpack buttons
        for (int bit = 0; bit < 24; bit++)
        {
            int byteIndex = bit / 8;
            int bitOffset = bit % 8;
            if ((ButtonBitfield[byteIndex] & (1 << bitOffset)) != 0)
            {
                if (bit < (int)Core.Domain.ControllerButton.MaxValue)
                {
                    state.SetButtonState((Core.Domain.ControllerButton)bit, Core.Domain.ButtonState.Pressed);
                }
            }
        }

        return state;
    }
}

/// <summary>
/// Discovery broadcast payload: hostname, IP address, port, and active controller name.
/// Used in DiscoveryResponse packets sent by the PC server.
/// </summary>
public class DiscoveryPayload
{
    public string HostName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; }
    public string ControllerName { get; set; } = "";

    /// <summary>
    /// Serializes discovery info into a compact 64-byte packet (same fixed size as all protocol messages).
    /// Layout: [0]=Version, [1]=MsgType(0x0B), [2..17]=HostName (16 bytes UTF8 truncated),
    ///         [18..33]=IP (16 bytes UTF8), [34..35]=Port (uint16 LE), [36..59]=ControllerName (24 bytes UTF8), [63]=XOR checksum.
    /// </summary>
    public byte[] Serialize()
    {
        byte[] buffer = new byte[ControllerMessage.PacketSize];
        buffer[0] = ProtocolVersion.Major;
        buffer[1] = (byte)MessageType.DiscoveryResponse;

        // HostName: bytes 2..17 (16 bytes max)
        var hostBytes = Encoding.UTF8.GetBytes(HostName ?? "");
        Array.Copy(hostBytes, 0, buffer, 2, Math.Min(hostBytes.Length, 16));

        // IP: bytes 18..33 (16 bytes max)
        var ipBytes = Encoding.UTF8.GetBytes(IpAddress ?? "");
        Array.Copy(ipBytes, 0, buffer, 18, Math.Min(ipBytes.Length, 16));

        // Port: bytes 34..35 (uint16 LE)
        Array.Copy(BitConverter.GetBytes((ushort)Port), 0, buffer, 34, 2);

        // ControllerName: bytes 36..59 (24 bytes max)
        var nameBytes = Encoding.UTF8.GetBytes(ControllerName ?? "");
        Array.Copy(nameBytes, 0, buffer, 36, Math.Min(nameBytes.Length, 24));

        // XOR checksum at byte 63
        byte checksum = 0;
        for (int i = 0; i < ControllerMessage.PacketSize - 1; i++)
            checksum ^= buffer[i];
        buffer[ControllerMessage.PacketSize - 1] = checksum;

        return buffer;
    }

    public static DiscoveryPayload Deserialize(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 36)
            throw new ArgumentException("Buffer too small for DiscoveryPayload", nameof(buffer));

        return new DiscoveryPayload
        {
            HostName = Encoding.UTF8.GetString(buffer, 2, 16).TrimEnd('\0'),
            IpAddress = Encoding.UTF8.GetString(buffer, 18, 16).TrimEnd('\0'),
            Port = BitConverter.ToUInt16(buffer, 34),
            ControllerName = buffer.Length >= 60 ? Encoding.UTF8.GetString(buffer, 36, 24).TrimEnd('\0') : ""
        };
    }
}

/// <summary>
/// Rumble/vibration command sent from mobile client to PC server.
/// Layout: [0]=Version, [1]=MsgType(0x0C), [2]=LeftMotor (0-255), [3]=RightMotor (0-255),
///         [4..5]=DurationMs (uint16 LE), [63]=XOR checksum.
/// </summary>
public class RumbleCommand
{
    public byte LeftMotor { get; set; }
    public byte RightMotor { get; set; }
    public ushort DurationMs { get; set; } = 200;

    public byte[] Serialize()
    {
        byte[] buffer = new byte[ControllerMessage.PacketSize];
        buffer[0] = ProtocolVersion.Major;
        buffer[1] = (byte)MessageType.Rumble;
        buffer[2] = LeftMotor;
        buffer[3] = RightMotor;
        Array.Copy(BitConverter.GetBytes(DurationMs), 0, buffer, 4, 2);

        byte checksum = 0;
        for (int i = 0; i < ControllerMessage.PacketSize - 1; i++)
            checksum ^= buffer[i];
        buffer[ControllerMessage.PacketSize - 1] = checksum;

        return buffer;
    }

    public static RumbleCommand Deserialize(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 6)
            throw new ArgumentException("Buffer too small for RumbleCommand", nameof(buffer));

        return new RumbleCommand
        {
            LeftMotor = buffer[2],
            RightMotor = buffer[3],
            DurationMs = BitConverter.ToUInt16(buffer, 4)
        };
    }
}
