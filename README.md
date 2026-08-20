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

## Architecture Overview

```
Physical Gamepad (USB/DirectInput/Ucom)
          ↓
    SDL2 Acquisition
          ↓
   Normalization (Deadzone, Calibration)
          ↓
   Mapping (User Profiles)
          ↓
  Canonical Controller State
          ↓
   Protocol (Binary, Compact)
          ↓
   Transport (WiFi UDP / Bluetooth)
          ↓
   Mobile Client (Android/iOS)
          ↓
   Visualization & Input Injection
```

## Technology Stack

**Desktop (PC/Linux/Mac)**:
- .NET 8 (C#)
- Avalonia (Cross-platform UI)
- SDL2-CS (Gamepad acquisition)
- Custom binary protocol over UDP

**Mobile (Android/iOS)**:
- Flutter (Cross-platform)
- UDP sockets for real-time communication
- QR code scanning for pairing

## Quick Start

### Desktop

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
- [Protocol Specification](docs/PROTOCOL.md) - Binary protocol format and message types
- [Development Guide](docs/DEVELOPMENT.md) - Building and extending the project
- [Testing](docs/TESTING.md) - Test strategy and running tests
- [Roadmap](docs/ROADMAP.md) - Development phases and feature status

## Project Structure

```
src/
  ├── ControllerBridge.Core/           Domain models and core logic
  ├── ControllerBridge.Acquisition/    Gamepad input backends
  ├── ControllerBridge.Transport/      Network transport and protocol
  └── ControllerBridge.Desktop/        Avalonia desktop application

flutter_client/                         Flutter mobile app

tests/                                  Unit and integration tests

docs/                                   Technical documentation
```

## Current Status

✅ Phase 1: Core acquisition and normalization  
✅ Phase 2: Mapping and profiles  
✅ Phase 3: Protocol specification  
✅ Phase 4: WiFi UDP transport  
✅ Phase 5: Mobile client (Android/iOS)  
✅ Phase 6: UI/UX polish  
⏳ Phase 7: Bluetooth HID (experimental)  

## Limitations & Known Issues

### Platform-Specific Constraints

**Input Injection**:
- **Android**: Standard gamepad event injection not available for most third-party apps. The client works as a diagnostic tool and for compatible receivers.
- **iOS**: Limited to compatible apps via Game Center or custom frameworks. Standard system-wide injection not available.
- **Windows/Linux**: Full support where gamepad APIs available.

**Bluetooth HID**:
- **Windows**: Limited support via native drivers; requires elevated permissions.
- **Linux**: Requires BlueZ daemon and appropriate permissions.
- **macOS**: Experimental via IOKit.

## Contributing

This project follows strict architectural principles:

- Domain logic is platform-agnostic
- Platform-specific code is isolated behind abstractions
- All features are real, not simulated
- Performance is measured, not assumed

## License

(To be determined)

## Support

For issues, feature requests, or technical questions, please refer to the [Development Guide](docs/DEVELOPMENT.md) and [Testing Guide](docs/TESTING.md).
