namespace ControllerBridge.Core.Mapping;

using Domain;

/// <summary>
/// Button remapping configuration.
/// </summary>
public class ButtonMapping
{
    public ControllerButton Source { get; set; }
    public ControllerButton Target { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Axis remapping configuration.
/// </summary>
public class AxisMapping
{
    public string Source { get; set; } = ""; // e.g., "LeftStickX"
    public string Target { get; set; } = ""; // e.g., "RightStickX"
    public bool Invert { get; set; }
    public float Sensitivity { get; set; } = 1.0f;
    public float DeadzoneThreshold { get; set; } = 0.1f;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// User-defined controller profile with custom mappings and settings.
/// </summary>
public class MappingProfile
{
    public string Name { get; set; } = "Default";
    public string ControllerId { get; set; } = "";
    public string ControllerName { get; set; } = "";

    // Button mappings (identity mapping by default)
    public List<ButtonMapping> ButtonMappings { get; set; } = new();

    // Axis mappings
    public List<AxisMapping> AxisMappings { get; set; } = new();

    // Global settings
    public float GlobalDeadzone { get; set; } = 0.1f;
    public float GlobalSensitivity { get; set; } = 1.0f;
    public float CurveExponent { get; set; } = 1.0f; // 1.0 = linear

    // Trigger settings
    public float LeftTriggerSensitivity { get; set; } = 1.0f;
    public float RightTriggerSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Create a default profile with identity mappings.
    /// </summary>
    public static MappingProfile CreateDefault(string controllerId, string controllerName)
    {
        var profile = new MappingProfile
        {
            Name = "Default",
            ControllerId = controllerId,
            ControllerName = controllerName
        };

        // Initialize button mappings (all identity)
        foreach (ControllerButton button in Enum.GetValues(typeof(ControllerButton)))
        {
            if (button != ControllerButton.MaxValue)
            {
                profile.ButtonMappings.Add(new ButtonMapping
                {
                    Source = button,
                    Target = button,
                    IsEnabled = true
                });
            }
        }

        return profile;
    }

    /// <summary>
    /// Get mapped button target (or identity if no mapping).
    /// </summary>
    public ControllerButton MapButton(ControllerButton source)
    {
        var mapping = ButtonMappings.FirstOrDefault(m => m.Source == source && m.IsEnabled);
        return mapping?.Target ?? source;
    }
}
