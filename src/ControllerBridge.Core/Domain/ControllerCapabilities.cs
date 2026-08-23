namespace ControllerBridge.Core.Domain;

/// <summary>
/// Describes the hardware capabilities and layout features of a gamepad.
/// </summary>
public class ControllerCapabilities
{
    public string Name { get; set; } = "Generic Controller";
    public int ButtonCount { get; set; } = 12;
    public int AxisCount { get; set; } = 4;

    // Face buttons
    public bool HasButton_A { get; set; } = true;
    public bool HasButton_B { get; set; } = true;
    public bool HasButton_X { get; set; } = true;
    public bool HasButton_Y { get; set; } = true;

    // Shoulders
    public bool HasButton_LB { get; set; } = true;
    public bool HasButton_RB { get; set; } = true;
    public bool HasLeftTrigger { get; set; } = true;
    public bool HasRightTrigger { get; set; } = true;
    public bool TriggersAreAnalog { get; set; } = true;

    // Sticks
    public bool HasLeftStick { get; set; } = true;
    public bool HasRightStick { get; set; } = true;
    public bool HasButton_LS { get; set; } = true;
    public bool HasButton_RS { get; set; } = true;

    // Menu / Navigation
    public bool HasButton_Start { get; set; } = true;
    public bool HasButton_Back { get; set; } = true;
    public bool HasButton_Guide { get; set; } = false;

    // D-Pad
    public bool HasDPad { get; set; } = true;
    public bool DPadIsButton { get; set; } = false;

    // Force Feedback / Rumble
    public bool HasVibration { get; set; } = false;
}
