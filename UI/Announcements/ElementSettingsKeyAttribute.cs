using System;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Overrides the element-settings key that AnnouncementRegistry would otherwise
/// derive from the class name. Use this when the class name reads awkwardly
/// for settings (e.g., <c>ProxyPotionHolder</c> → settings key <c>"potion"</c>
/// rather than <c>"potion_holder"</c>) or when the key needs to match an
/// existing key from another subsystem (e.g., FocusStringSettings).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ElementSettingsKeyAttribute : Attribute
{
    public string Key { get; }

    public ElementSettingsKeyAttribute(string key)
    {
        Key = key;
    }
}
