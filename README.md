# HTUP Controller Bridge (Ulrich PadLink)

**Transform any USB/DirectInput/Ucom gamepad into a wireless mobile controller.**

HTUP Controller Bridge is a cross-platform application that captures gamepad input from desktop (PC/Linux/Mac), normalizes it, and streams it to mobile devices (Android/iOS) via Wi-Fi UDP or Bluetooth HID.

## Features

- 🎮 **Universal Gamepad Support**: DirectInput, HID generic, XInput, legacy Ucom controllers
- 📡 **Low-Latency Wi-Fi Transport**: UDP-based real-time streaming (~120 Hz target)
- 📱 **Mobile Client**: Android and iOS apps with live gamepad visualization
- ⚙️ **Advanced Normalization**: Deadzone, calibration, axis curves, remapping
- 📊 **Real-Time Diagnostics**: Packet loss, jitter, latency, connection metrics
- 🎨 **Premium UI**: Modern, professional desktop and mobile interfaces
- 🔗 **Easy Pairing**: QR code-based connection establishment
- 🔋 **Bluetooth HID** (experimental): Support where technically feasible

## Quick Start

### Desktop (Windows/Linux/macOS)

```bash
cd src/ControllerBridge.Desktop
dotnet run
```

### Mobile (Android)

```bash
cd flutter_client
flutter pub get
flutter run -d android
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Protocol Specification](docs/PROTOCOL.md)
- [Development Guide](docs/DEVELOPMENT.md)

## Status

✅ Phase 1-5: Core, WiFi Transport, Mobile Client  
🔄 Phase 6: UI Polish  
⏳ Phase 7: Bluetooth HID (Experimental)  
