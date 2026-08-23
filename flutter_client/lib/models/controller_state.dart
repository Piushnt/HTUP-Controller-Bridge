import 'dart:typed_data';

/// Boutons canoniques de la manette
enum GamepadButton {
  a,
  b,
  x,
  y,
  lb,
  rb,
  ltButton,
  rtButton,
  ls,
  rs,
  start,
  back,
  dPadUp,
  dPadDown,
  dPadLeft,
  dPadRight,
  guide,
}

/// État complet d'une manette reçu et normalisé
class MobileControllerState {
  final int sequenceNumber;
  final int timestampMs;
  final double leftStickX;
  final double leftStickY;
  final double rightStickX;
  final double rightStickY;
  final double leftTrigger;
  final double rightTrigger;
  final int dPadX; // -1, 0, 1
  final int dPadY; // -1, 0, 1
  final bool isConnected;
  final Set<GamepadButton> pressedButtons;

  MobileControllerState({
    this.sequenceNumber = 0,
    this.timestampMs = 0,
    this.leftStickX = 0.0,
    this.leftStickY = 0.0,
    this.rightStickX = 0.0,
    this.rightStickY = 0.0,
    this.leftTrigger = 0.0,
    this.rightTrigger = 0.0,
    this.dPadX = 0,
    this.dPadY = 0,
    this.isConnected = false,
    Set<GamepadButton>? pressedButtons,
  }) : pressedButtons = pressedButtons ?? {};

  bool isPressed(GamepadButton button) => pressedButtons.contains(button);

  /// Décode la trame binaire 64 octets émise par ControllerMessage.Serialize()
  factory MobileControllerState.fromBinary(Uint8List bytes) {
    if (bytes.length < 41) {
      throw FormatException('Taille de paquet UDP invalide: ${bytes.length} octets');
    }

    final byteData = ByteData.sublistView(bytes);
    int offset = 0;

    // Header
    final protocolVersion = byteData.getUint8(offset++);
    final messageType = byteData.getUint8(offset++);
    final sequenceNumber = byteData.getUint32(offset, Endian.little);
    offset += 4;

    final timestampMs = byteData.getUint64(offset, Endian.little);
    offset += 8;

    // Buttons (3 octets = 24 bits)
    final btnByte0 = byteData.getUint8(offset++);
    final btnByte1 = byteData.getUint8(offset++);
    final btnByte2 = byteData.getUint8(offset++);
    final btnBitfield = btnByte0 | (btnByte1 << 8) | (btnByte2 << 16);

    // Analog axes (4 x float32 Little Endian)
    final leftStickX = byteData.getFloat32(offset, Endian.little);
    offset += 4;
    final leftStickY = byteData.getFloat32(offset, Endian.little);
    offset += 4;
    final rightStickX = byteData.getFloat32(offset, Endian.little);
    offset += 4;
    final rightStickY = byteData.getFloat32(offset, Endian.little);
    offset += 4;

    // Triggers (2 x uint8 -> 0.0 à 1.0)
    final rawLeftTrigger = byteData.getUint8(offset++);
    final rawRightTrigger = byteData.getUint8(offset++);
    final leftTrigger = rawLeftTrigger / 255.0;
    final rightTrigger = rawRightTrigger / 255.0;

    // D-Pad (1 octet encodé)
    final dPadByte = byteData.getUint8(offset++);
    final dPadX = (dPadByte & 0x03) - 1;
    final dPadY = ((dPadByte >> 2) & 0x03) - 1;

    // Flags
    final flags = byteData.getUint8(offset++);
    final isConnected = (flags & 0x01) != 0;

    // Décoder les boutons pressés
    final pressed = <GamepadButton>{};
    for (int i = 0; i < GamepadButton.values.length; i++) {
      if ((btnBitfield & (1 << i)) != 0) {
        pressed.add(GamepadButton.values[i]);
      }
    }

    return MobileControllerState(
      sequenceNumber: sequenceNumber,
      timestampMs: timestampMs,
      leftStickX: leftStickX.clamp(-1.0, 1.0),
      leftStickY: leftStickY.clamp(-1.0, 1.0),
      rightStickX: rightStickX.clamp(-1.0, 1.0),
      rightStickY: rightStickY.clamp(-1.0, 1.0),
      leftTrigger: leftTrigger.clamp(0.0, 1.0),
      rightTrigger: rightTrigger.clamp(0.0, 1.0),
      dPadX: dPadX.clamp(-1, 1),
      dPadY: dPadY.clamp(-1, 1),
      isConnected: isConnected,
      pressedButtons: pressed,
    );
  }
}
