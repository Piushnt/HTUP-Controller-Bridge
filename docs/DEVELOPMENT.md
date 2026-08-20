# HTUP Controller Bridge - Development Guide

A comprehensive guide for developers working on the HTUP Controller Bridge project.

## Project Overview

HTUP Controller Bridge is a modular, cross-platform gamepad input bridging system built with C# and .NET 8.0. It enables seamless controller input acquisition, normalization, remapping, and real-time WiFi streaming to mobile clients.

## Development Environment Setup

### Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Git** for version control
- **Optional:** Docker for containerized builds

### Getting Started

```bash
# Clone the repository
git clone https://github.com/Piushnt/HTUP-Controller-Bridge.git
cd HTUP-Controller-Bridge

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the desktop application
dotnet run --project src/ControllerBridge.Desktop
```

## Project Structure

### Core Layer (`src/ControllerBridge.Core`)

The foundation of the system containing all domain models and core algorithms.

**Key Classes:**
- `ControllerState` - Canonical gamepad state representation
- `ControllerButton` - Enum of all supported buttons
- `AnalogAxis` - Analog stick/trigger data with history
- `ControllerCapabilities` - Device capability metadata

**Responsibilities:**
- Define canonical state models
- Implement input normalization algorithms
- Manage mapping profiles
- Provide diagnostics data structures

**Add new core features:**
1. Create a new folder (e.g., `Features/YourFeature`)
2. Implement with appropriate abstractions (interfaces)
3. Add unit tests to `tests/ControllerBridge.Core.Tests`
4. Document public APIs with XML comments

### Acquisition Layer (`src/ControllerBridge.Acquisition`)

Platform-specific gamepad input backends.

**Interface:**
```csharp
public interface IControllerBackend
{
    Task InitializeAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
    List<ControllerInfo> GetConnectedControllers();
    ControllerState? GetControllerState(string controllerId);
    
    event EventHandler<ControllerInfo>? ControllerConnected;
    event EventHandler<string>? ControllerDisconnected;
    event EventHandler<ControllerState>? ControllerStateChanged;
}
```

**Existing Backends:**
- `SimulatedControllerBackend` - Test implementation with synthetic data

**Implementing a new backend:**

1. Create a new folder: `src/ControllerBridge.Acquisition/Platforms/YourPlatform`
2. Implement `IControllerBackend`:
   ```csharp
   public class YourPlatformBackend : IControllerBackend
   {
       // Implementation
   }
   ```
3. Register in dependency injection (when DI container is added)
4. Add integration tests

**Example: Windows Native Input Backend**
```csharp
public class WindowsNativeBackend : IControllerBackend
{
    // Use Raw Input API (GetRawInputDeviceList, etc.)
    // Poll HID devices and parse state
}
```

### Transport Layer (`src/ControllerBridge.Transport`)

Network protocol and WiFi streaming.

**Key Components:**

**Protocol** (`Protocol/ControllerMessage.cs`)
- Binary message format (64 bytes)
- Serialization/deserialization
- Version management

**WiFi Server** (`WiFi/UdpControllerServer.cs`)
- UDP server listening on configurable port
- Broadcasts state to connected clients
- Tracks client connections and metrics

**QR Code** (`WiFi/QrCodeGenerator.cs`)
- Generates connection info as QR code
- PNG or SVG output formats
- Network discovery helpers

**Adding transport features:**
1. Extend `ControllerMessage` format (update protocol version)
2. Add new message types to `MessageType` enum
3. Update serialization/deserialization logic
4. Add protocol tests

### Desktop Application (`src/ControllerBridge.Desktop`)

User interface built with Avalonia (cross-platform XAML).

**Structure:**
```
Views/              # XAML UI components
  ├── MainWindow.axaml
  └── [Future: Controllers, Network, Settings views]
ViewModels/         # Reactive view models (BindingContext)
Models/             # UI-specific data models
App.axaml           # Application root
Program.cs          # Entry point
```

**Developing UI:**

1. Create new view:
   ```xaml
   <Window xmlns="https://github.com/avaloniaui" ...>
       <StackPanel>
           <!-- Your UI here -->
       </StackPanel>
   </Window>
   ```

2. Create code-behind:
   ```csharp
   public partial class YourWindow : Window
   {
       public YourWindow()
       {
           InitializeComponent();
       }
   }
   ```

3. Bind to data (future: MVVM with ViewModels)

### Testing (`tests/`)

**Test Projects:**
- `ControllerBridge.Core.Tests` - Unit tests for core logic
- `ControllerBridge.Transport.Tests` - Protocol and network tests

**Testing Guidelines:**

1. **Unit Tests** (xUnit):
   ```csharp
   [Fact]
   public void MethodName_Condition_ExpectedResult()
   {
       // Arrange
       var input = new Data();
       
       // Act
       var result = SomeClass.DoSomething(input);
       
       // Assert
       Assert.Equal(expected, result);
   }
   ```

2. **Test Naming:** `MethodName_Condition_ExpectedResult`

3. **Coverage Goals:**
   - Aim for >80% coverage on business logic
   - Test edge cases and error conditions
   - Use mocks/stubs for external dependencies

4. **Running Tests:**
   ```bash
   # Run all tests
   dotnet test
   
   # Run specific project
   dotnet test tests/ControllerBridge.Core.Tests
   
   # Filter by name
   dotnet test --filter "MethodName"
   
   # Verbose output
   dotnet test --logger "console;verbosity=detailed"
   ```

## Code Style & Conventions

### Naming Conventions
- **Classes/Methods/Properties:** PascalCase
- **Private fields:** `_camelCase`
- **Local variables:** `camelCase`
- **Constants:** `UPPER_CASE` or `PascalCase`

### File Organization
- One public class per file (except small related classes)
- File name matches class name
- Group related classes in folders

### Documentation
- Use XML documentation comments on public APIs:
  ```csharp
  /// <summary>
  /// Brief description of what this does.
  /// </summary>
  /// <param name="param1">Description of param1</param>
  /// <returns>Description of return value</returns>
  public ControllerState GetState(string id)
  {
      // Implementation
  }
  ```

### Code Style
- Use `var` for obvious types, explicit types otherwise
- Prefer LINQ over loops when readable
- Use nullable reference types (enabled in .csproj)
- Follow async/await patterns

## Building & Deployment

### Debug Build
```bash
dotnet build -c Debug
```

### Release Build
```bash
dotnet build -c Release
```

### Publish Desktop App
```bash
# Self-contained (includes .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained

# Framework-dependent (requires .NET installed)
dotnet publish -c Release
```

### Run Desktop App
```bash
dotnet run --project src/ControllerBridge.Desktop
```

## Debugging

### Visual Studio Debug
1. Open solution in Visual Studio
2. Set breakpoints
3. Press F5 to start debugging
4. Step through code with F10/F11

### Console Debug Output
```csharp
System.Diagnostics.Debug.WriteLine($"Value: {value}");
```

### Logging
The project uses `Microsoft.Extensions.Logging`:
```csharp
private readonly ILogger<MyClass> _logger;

_logger.LogInformation("Message with {param}", paramValue);
_logger.LogError(ex, "Error occurred: {message}", message);
```

## Git Workflow

### Branch Strategy
- `main` - Production-ready code
- `dev` - Development branch (default)
- `feature/description` - Feature branches

### Commit Guidelines
- Use clear, descriptive commit messages
- Use imperative mood: "Add feature" not "Added feature"
- Reference issues: "Fix #123"
- Keep commits focused and atomic

### Example
```bash
git checkout -b feature/controller-remapping
# ... make changes ...
git add .
git commit -m "Add button remapping profile system"
git push origin feature/controller-remapping
# Create pull request on GitHub
```

## Common Development Tasks

### Adding a New Button Type
1. Add to `ControllerButton` enum in `Core/Domain/ControllerButton.cs`
2. Update `ControllerState` if needed
3. Update button packing in `ControllerMessage.cs`
4. Add tests

### Implementing Platform Backend
1. Create folder: `src/ControllerBridge.Acquisition/Platforms/NewPlatform`
2. Implement `IControllerBackend`
3. Handle platform-specific input APIs
4. Add integration tests
5. Register in startup (future DI)

### Adding Network Metrics
1. Extend `NetworkMetrics` class in `Core/Diagnostics`
2. Update `UdpControllerServer` to collect metric
3. Expose via property or event
4. Update UI to display (when UI exists)

## Performance Optimization

### Key Metrics
- USB acquisition latency: <5ms
- WiFi streaming frequency: 60-120 Hz
- Packet size: ~64 bytes
- Memory usage: ~50 MB base

### Profiling
1. Use Visual Studio Profiler (Debug > Performance Profiler)
2. Measure critical paths (input loop, serialization)
3. Monitor allocations (GC pressure)
4. Test under load (many clients)

### Optimization Tips
- Reuse buffer allocations where possible
- Use `Span<T>` for zero-copy operations
- Profile before optimizing
- Balance readability with performance

## Troubleshooting

### Build Issues

**NuGet restore failures:**
```bash
dotnet nuget locals all --clear
dotnet restore
```

**Project reference errors:**
- Verify project paths in .sln file
- Check .csproj dependencies
- Rebuild solution

### Runtime Issues

**Missing dependencies:**
```bash
dotnet add package PackageName
```

**Port already in use (UDP):**
- Change UDP server port in configuration
- Kill process using port: `lsof -i :5555`

**Simulated controller not updating:**
- Check simulation loop in `SimulatedControllerBackend`
- Verify event handlers are registered
- Check cancellation token

## Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [xUnit Documentation](https://xunit.net/)

## Getting Help

1. Check existing issues/discussions on GitHub
2. Review similar implementations in the codebase
3. Run tests to see expected behavior
4. Ask in project discussions

## Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Make changes and add tests
4. Run full test suite: `dotnet test`
5. Commit: `git commit -m "Add my feature"`
6. Push: `git push origin feature/my-feature`
7. Open Pull Request with clear description

---

**Happy coding! 🚀**
