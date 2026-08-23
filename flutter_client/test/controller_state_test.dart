import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_client/models/controller_state.dart';

void main() {
  group('MobileControllerState Binary Protocol Tests', () {
    test('Correctly deserializes 64-byte payload from .NET Bridge', () {
      final buffer = Uint8List(64);
      final byteData = ByteData.sublistView(buffer);

      int offset = 0;
      // Header: Version 1, Type 3 (ControllerState), Seq 42
      byteData.setUint8(offset++, 1);
      byteData.setUint8(offset++, 3);
      byteData.setUint32(offset, 42, Endian.little);
      offset += 4;

      // Timestamp
      byteData.setUint64(offset, 123456, Endian.little);
      offset += 8;

      // Buttons (A pressed = bit 0, RB pressed = bit 5)
      // Bit 0 = 0x01, Bit 5 = 0x20 -> 0x21
      byteData.setUint8(offset++, 0x21);
      byteData.setUint8(offset++, 0x00);
      byteData.setUint8(offset++, 0x00);

      // Analog axes
      byteData.setFloat32(offset, 0.75, Endian.little); // LeftStickX
      offset += 4;
      byteData.setFloat32(offset, -0.50, Endian.little); // LeftStickY
      offset += 4;
      byteData.setFloat32(offset, 0.0, Endian.little); // RightStickX
      offset += 4;
      byteData.setFloat32(offset, 1.0, Endian.little); // RightStickY
      offset += 4;

      // Triggers: Left=128 (0.5), Right=255 (1.0)
      byteData.setUint8(offset++, 128);
      byteData.setUint8(offset++, 255);

      // D-Pad: X=1, Y=-1 -> packedX=2 (binary 10), packedY=0 (binary 00) -> 0x02
      byteData.setUint8(offset++, 0x02);

      // Flags: connected = 1
      byteData.setUint8(offset++, 0x01);

      // Client ID
      byteData.setUint32(offset, 999, Endian.little);

      // Decode
      final state = MobileControllerState.fromBinary(buffer);

      expect(state.sequenceNumber, equals(42));
      expect(state.timestampMs, equals(123456));
      expect(state.leftStickX, closeTo(0.75, 0.001));
      expect(state.leftStickY, closeTo(-0.50, 0.001));
      expect(state.rightStickX, closeTo(0.0, 0.001));
      expect(state.rightStickY, closeTo(1.0, 0.001));
      expect(state.leftTrigger, closeTo(128 / 255.0, 0.01));
      expect(state.rightTrigger, closeTo(1.0, 0.01));
      expect(state.dPadX, equals(1));
      expect(state.dPadY, equals(-1));
      expect(state.isConnected, isTrue);
      expect(state.isPressed(GamepadButton.a), isTrue);
      expect(state.isPressed(GamepadButton.rb), isTrue);
      expect(state.isPressed(GamepadButton.b), isFalse);
    });
  });
}
