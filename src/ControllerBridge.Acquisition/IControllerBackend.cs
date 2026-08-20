namespace ControllerBridge.Acquisition;

using ControllerBridge.Core.Domain;

/// <summary>
/// Interface for controller input acquisition backends.
/// Implementations handle platform-specific gamepad input reading.
/// </summary>
public interface IControllerBackend
{
    /// <summary>
    /// Initialize the backend.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Shutdown the backend.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    /// Get list of connected controllers.
    /// </summary>
    List<ControllerInfo> GetConnectedControllers();

    /// <summary>
    /// Get current state of a specific controller.
    /// </summary>
    ControllerState? GetControllerState(string controllerId);

    /// <summary>
    /// Event raised when a controller is connected.
    /// </summary>
    event EventHandler<ControllerInfo>? ControllerConnected;

    /// <summary>
    /// Event raised when a controller is disconnected.
    /// </summary>
    event EventHandler<string>? ControllerDisconnected;

    /// <summary>
    /// Event raised when controller state changes.
    /// </summary>
    event EventHandler<ControllerState>? ControllerStateChanged;
}

/// <summary>
/// Basic controller information.
/// </summary>
public class ControllerInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public ControllerCapabilities Capabilities { get; set; } = new();
}
