![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Flutter](https://img.shields.io/badge/Flutter-3.x-02569B?logo=flutter)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-lightgrey)
![License](https://img.shields.io/badge/License-MIT-blue.svg)


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
- 🔵 **Bluetooth HID** (experimental): Support where technically feasible

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

### Mobile (iOS)

```bash
cd flutter_client
flutter pub get
flutter run -d ios
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md) - System design and module responsibilities
- [Protocol Specification](docs/PROTOCOL.md) - Binary protocol format
- [Development Guide](docs/DEVELOPMENT.md) - Building and extending
- [Testing Guide](docs/TESTING.md) - Test strategy
- [Roadmap](docs/ROADMAP.md) - Development phases

## Project Structure

```
src/
  ├── ControllerBridge.Core/           Domain models and core logic
  ├── ControllerBridge.Acquisition/    Gamepad input backends
  ├── ControllerBridge.Transport/      Network transport and protocol
  └── ControllerBridge.Desktop/        Avalonia desktop application

flutter_client/                        Flutter mobile app
tests/                                 Unit and integration tests
docs/                                  Technical documentation
```

## Status

✅ Phase 1-6: Core, WiFi, Mobile, UI Polish  
🔄 Phase 7: Bluetooth HID (Experimental)  

## License

(To be determined)
