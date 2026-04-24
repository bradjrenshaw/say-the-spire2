using System.Collections.Generic;

namespace SayTheSpire2.Settings;

/// <summary>
/// Registers the per-type-key "announce position" toggle (whether containers
/// append "X of Y" position info for elements of this type). The old
/// type / subtype / tooltip announce toggles were deleted along with the legacy
/// focus-string shim; Phase 3 will re-introduce granular announcement toggles
/// via the announcement pipeline.
/// </summary>
public static class FocusStringSettings
{
    private static readonly HashSet<string> _registeredKeys = new();

    /// <summary>Registers the announce_position toggle for a UI element type key.</summary>
    public static void Register(string typeKey, string displayName)
    {
        if (!_registeredKeys.Add(typeKey)) return;

        var category = ModSettingsRegistry.EnsureCategory($"ui.{typeKey}", $"UI/{displayName}");
        category.Add(new BoolSetting("announce_position", "Announce Position", true));
    }

    public static void RegisterDefaults()
    {
        Register("card", "Card");
        Register("button", "Button");
        Register("relic", "Relic");
        Register("potion", "Potion");
        Register("orb", "Orb");
        Register("creature", "Creature");
        Register("checkbox", "Checkbox");
        Register("slider", "Slider");
        Register("dropdown", "Dropdown");
        Register("keybind", "Key Binding");
        Register("shop_item", "Shop Item");
        Register("map_node", "Map Node");
    }

    public static bool ShouldAnnouncePosition(string typeKey)
    {
        if (!_registeredKeys.Contains(typeKey)) return true;
        return ModSettings.GetValue<bool>($"ui.{typeKey}.announce_position");
    }
}
