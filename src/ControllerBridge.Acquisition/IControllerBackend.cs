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

    /// <summary>
    /// Whether this backend supports vibration / force feedback.
    /// Defaults to false for safe behavior on generic controllers.
    /// </summary>
    bool SupportsVibration => false;

    /// <summary>
    /// Sends a vibration/rumble command to the specified controller.
    /// Must be fault-tolerant: if the hardware doesn't support vibration,
    /// this must silently no-op without throwing or blocking.
    /// </summary>
    /// <param name="controllerId">Target controller ID.</param>
    /// <param name="leftMotor">Left motor intensity [0.0 - 1.0].</param>
    /// <param name="rightMotor">Right motor intensity [0.0 - 1.0].</param>
    /// <param name="durationMs">Vibration duration in milliseconds.</param>
    Task SetVibrationAsync(string controllerId, float leftMotor, float rightMotor, ushort durationMs = 200) => Task.CompletedTask;
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
