namespace ControllerBridge.Core.Domain;

/// <summary>
/// Canonical controller state - platform-independent representation of gamepad input.
/// This is the unified state that all backends normalize to.
/// </summary>
public class ControllerState
{
    /// <summary>Unique controller identifier (e.g., USB vendor ID + product ID + serial).</summary>
    public string ControllerId { get; set; } = "";

    /// <summary>Human-readable controller name.</summary>
    public string ControllerName { get; set; } = "";

    /// <summary>Sequence number for packet ordering and loss detection.</summary>
    public uint SequenceNumber { get; set; }

    /// <summary>Timestamp in milliseconds since last reset.</summary>
    public ulong TimestampMs { get; set; }

    /// <summary>Controller is currently connected and providing input.</summary>
    public bool IsConnected { get; set; }

    // Button states
    private Dictionary<ControllerButton, ButtonState> _buttonStates = new();

    // Analog axes
    public AnalogAxis LeftStickX { get; set; }
    public AnalogAxis LeftStickY { get; set; }
    public AnalogAxis RightStickX { get; set; }
    public AnalogAxis RightStickY { get; set; }

    // Triggers (0.0 to 1.0)
    public float LeftTrigger { get; set; }
    public float RightTrigger { get; set; }

    // D-Pad
    public int DPadX { get; set; } // -1, 0, 1
    public int DPadY { get; set; } // -1, 0, 1

    // Controller capabilities
    public ControllerCapabilities Capabilities { get; set; } = new();

    /// <summary>Get button state.</summary>
    public ButtonState GetButtonState(ControllerButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) ? state : ButtonState.Released;
    }

    /// <summary>Set button state.</summary>
    public void SetButtonState(ControllerButton button, ButtonState state)
    {
        _buttonStates[button] = state;
    }

    /// <summary>Get all currently pressed buttons.</summary>
    public List<ControllerButton> GetPressedButtons()
    {
        return _buttonStates
            .Where(kvp => kvp.Value == ButtonState.Pressed)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>Clear all state (reset to default).</summary>
    public void Reset()
    {
        _buttonStates.Clear();
        LeftStickX = new AnalogAxis();
        LeftStickY = new AnalogAxis();
        RightStickX = new AnalogAxis();
        RightStickY = new AnalogAxis();
        LeftTrigger = 0f;
        RightTrigger = 0f;
        DPadX = 0;
        DPadY = 0;
    }

    /// <summary>Create a deep copy of this state.</summary>
    public ControllerState Clone()
    {
        return new ControllerState
        {
            ControllerId = ControllerId,
            ControllerName = ControllerName,
            SequenceNumber = SequenceNumber,
            TimestampMs = TimestampMs,
            IsConnected = IsConnected,
            _buttonStates = new Dictionary<ControllerButton, ButtonState>(_buttonStates),
            LeftStickX = LeftStickX,
            LeftStickY = LeftStickY,
            RightStickX = RightStickX,
            RightStickY = RightStickY,
            LeftTrigger = LeftTrigger,
            RightTrigger = RightTrigger,
            DPadX = DPadX,
            DPadY = DPadY,
            Capabilities = Capabilities
        };
    }
}
