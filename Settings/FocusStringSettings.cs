using System.Collections.Generic;

namespace SayTheSpire2.Settings;

/// <summary>
/// Registers per-type-key settings for which parts of the focus string
/// are announced (type, subtype, tooltip). Settings are placed under the
/// existing ui.{typeKey} category so they appear alongside any element-
/// specific settings (e.g., card verbose costs).
/// </summary>
public static class FocusStringSettings
{
    private static readonly HashSet<string> _registeredKeys = new();
    private static readonly HashSet<string> _subtypeKeys = new();

    /// <summary>
    /// Register focus string toggles for a UI element type key.
    /// </summary>
    /// <param name="typeKey">Stable key matching GetTypeKey() (e.g., "card")</param>
    /// <param name="displayName">Human-readable name for the settings category (e.g., "Card")</param>
    /// <param name="hasSubtype">Whether to include an announce_subtype toggle</param>
    public static void Register(string typeKey, string displayName, bool hasSubtype = false)
    {
        if (!_registeredKeys.Add(typeKey)) return;

        var category = ModSettingsRegistry.EnsureCategory($"ui.{typeKey}", $"UI/{displayName}");

        category.Add(new BoolSetting("announce_type", "Announce Type", true));
        if (hasSubtype)
        {
            category.Add(new BoolSetting("announce_subtype", "Announce Subtype", true));
            _subtypeKeys.Add(typeKey);
        }
        category.Add(new BoolSetting("announce_tooltip", "Announce Tooltip", true));
        category.Add(new BoolSetting("announce_position", "Announce Position", true));
    }

    public static void RegisterDefaults()
    {
        Register("card", "Card", hasSubtype: true);
        Register("button", "Button");
        Register("relic", "Relic");
        Register("potion", "Potion");
        Register("orb", "Orb");
        Register("creature", "Creature");
        Register("checkbox", "Checkbox");
        Register("slider", "Slider");
        Register("dropdown", "Dropdown");
        Register("keybind", "Key Binding");
        Register("shop item", "Shop Item");
        Register("map node", "Map Node");
    }

    public static bool ShouldAnnounceType(string typeKey)
    {
        if (!_registeredKeys.Contains(typeKey)) return true;
        return ModSettings.GetValue<bool>($"ui.{typeKey}.announce_type");
    }

    public static bool ShouldAnnounceSubtype(string typeKey)
    {
        if (!_subtypeKeys.Contains(typeKey)) return true;
        return ModSettings.GetValue<bool>($"ui.{typeKey}.announce_subtype");
    }

    public static bool ShouldAnnounceTooltip(string typeKey)
    {
        if (!_registeredKeys.Contains(typeKey)) return true;
        return ModSettings.GetValue<bool>($"ui.{typeKey}.announce_tooltip");
    }

    public static bool ShouldAnnouncePosition(string typeKey)
    {
        if (!_registeredKeys.Contains(typeKey)) return true;
        return ModSettings.GetValue<bool>($"ui.{typeKey}.announce_position");
    }
}
