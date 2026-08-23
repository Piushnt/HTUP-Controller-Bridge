namespace ControllerBridge.Core.Mapping;

using Domain;
using System.Text.Json;

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
    public string FileName { get; set; } = "Default_Ucom.json";
    public string ControllerId { get; set; } = "";
    public string ControllerName { get; set; } = "";

    // Button mappings (identity mapping by default)
    public List<ButtonMapping> ButtonMappings { get; set; } = new();

    // Axis mappings
    public List<AxisMapping> AxisMappings { get; set; } = new();

    // Global settings
    public float GlobalDeadzone { get; set; } = 0.15f;
    public float GlobalSensitivity { get; set; } = 1.35f;
    public float CurveExponent { get; set; } = 1.0f; // 1.0 = linear

    // Trigger settings
    public float LeftTriggerSensitivity { get; set; } = 1.0f;
    public float RightTriggerSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Create a default profile with identity mappings.
    /// </summary>
    public static MappingProfile CreateDefault(string controllerId = "generic-001", string controllerName = "Generic Controller")
    {
        var profile = new MappingProfile
        {
            Name = "Default Ucom",
            FileName = "Default_Ucom.json",
            ControllerId = controllerId,
            ControllerName = controllerName,
            GlobalDeadzone = 0.15f,
            GlobalSensitivity = 1.0f,
            CurveExponent = 1.0f
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
    /// Creates an optimized profile for Call of Duty: Mobile with high responsiveness and custom deadzones.
    /// </summary>
    public static MappingProfile CreateCodMobileProfile(string controllerId = "ucom-cod", string controllerName = "Ucom COD Mobile")
    {
        var profile = CreateDefault(controllerId, controllerName);
        profile.Name = "Call of Duty: Mobile";
        profile.FileName = "COD_Mobile.json";
        profile.GlobalDeadzone = 0.12f;
        profile.GlobalSensitivity = 1.35f;
        profile.CurveExponent = 1.1f;
        return profile;
    }

    /// <summary>
    /// Creates an optimized profile for Delta Force with precise aiming response.
    /// </summary>
    public static MappingProfile CreateDeltaForceProfile(string controllerId = "ucom-delta", string controllerName = "Ucom Delta Force")
    {
        var profile = CreateDefault(controllerId, controllerName);
        profile.Name = "Delta Force";
        profile.FileName = "Delta_Force.json";
        profile.GlobalDeadzone = 0.10f;
        profile.GlobalSensitivity = 1.25f;
        profile.CurveExponent = 1.05f;
        return profile;
    }

    /// <summary>
    /// Creates an optimized profile for generic Ucom USB double-shock gamepads.
    /// </summary>
    public static MappingProfile CreateUcomProfile(string controllerId = "ucom-generic", string controllerName = "Ucom Dual Gamepad")
    {
        var profile = CreateDefault(controllerId, controllerName);
        profile.Name = "Ucom Classic Gamepad";
        profile.FileName = "Default_Ucom.json";
        profile.GlobalDeadzone = 0.15f;
        profile.GlobalSensitivity = 1.35f;
        profile.CurveExponent = 1.0f;
        return profile;
    }

    /// <summary>
    /// Remaps a physical source button to a virtual Xbox target button.
    /// </summary>
    public void RemapButton(ControllerButton virtualTarget, ControllerButton physicalSource)
    {
        var existing = ButtonMappings.FirstOrDefault(m => m.Target == virtualTarget);
        if (existing != null)
        {
            existing.Source = physicalSource;
            existing.IsEnabled = true;
        }
        else
        {
            ButtonMappings.Add(new ButtonMapping
            {
                Target = virtualTarget,
                Source = physicalSource,
                IsEnabled = true
            });
        }
    }

    /// <summary>
    /// Get mapped button target (or identity if no mapping).
    /// </summary>
    public ControllerButton MapButton(ControllerButton source)
    {
        var mapping = ButtonMappings.FirstOrDefault(m => m.Source == source && m.IsEnabled);
        return mapping?.Target ?? source;
    }

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static MappingProfile? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MappingProfile>(json);
        }
        catch
        {
            return null;
        }
    }
}
