namespace ControllerBridge.Core.Domain;

/// <summary>
/// Standard gamepad button identifiers, mapping common controllers.
/// </summary>
public enum ControllerButton
{
    // Face buttons
    A,
    B,
    X,
    Y,

    // Shoulder buttons
    LB,
    RB,
    LT_Button,
    RT_Button,

    // Stick clicks
    LS,
    RS,

    // Menu buttons
    Start,
    Back,

    // D-Pad (represented as single buttons, or as axis values)
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,

    // Guide/Home button
    Guide,

    // Count for validation
    MaxValue
}

/// <summary>
/// Button state (pressed/released).
/// </summary>
public enum ButtonState
{
    Released = 0,
    Pressed = 1
}
