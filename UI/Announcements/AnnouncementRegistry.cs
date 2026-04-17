using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Registers announcement settings at startup.
///
/// <para>Global settings: every concrete <see cref="Announcement"/> subclass
/// gets an entry under <c>announcements.{key}/</c> with an "enabled"
/// BoolSetting plus anything declared in an optional static
/// <c>RegisterSettings(CategorySetting)</c> method.</para>
///
/// <para>Per-element overrides: every UIElement subclass with
/// <c>[AnnouncementOrder]</c> gets a corresponding tree under
/// <c>ui.{element}.announcements.{key}/</c>. Each override is a
/// <see cref="NullableBoolSetting"/> that inherits from the global by default
/// and gives the user a per-context toggle without forcing them to see
/// tri-state UI.</para>
///
/// <para>Keys are derived from class names: <c>HpAnnouncement</c> → <c>hp</c>,
/// <c>MonsterIntentsAnnouncement</c> → <c>monster_intents</c>,
/// <c>ProxyCreature</c> → <c>creature</c>, <c>ProxyRelicHolder</c> →
/// <c>relic_holder</c>, etc.</para>
/// </summary>
public static class AnnouncementRegistry
{
    private const string EnabledLocKey = "SETTINGS.ANNOUNCEMENT.ENABLED";
    private const string RootLocKey = "SETTINGS.ANNOUNCEMENTS_ROOT";

    public static void RegisterDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Globals first — per-element overrides reference them as fallbacks.
        foreach (var type in assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Announcement).IsAssignableFrom(t)))
        {
            try { RegisterGlobal(type); }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Announcement registration failed for {type.Name}: {e.Message}");
            }
        }

        // Per-element overrides for every element type with [AnnouncementOrder].
        foreach (var elementType in assembly.GetTypes()
            .Where(t => !t.IsAbstract
                     && typeof(UIElement).IsAssignableFrom(t)
                     && t.GetCustomAttribute<AnnouncementOrderAttribute>() != null))
        {
            try { RegisterElementOverrides(elementType); }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Per-element override registration failed for {elementType.Name}: {e.Message}");
            }
        }
    }

    private static void RegisterGlobal(Type announcementType)
    {
        var key = DeriveAnnouncementKey(announcementType);
        var displayName = DeriveDisplayName(StripSuffix(announcementType.Name, "Announcement"));
        var categoryLocKey = $"SETTINGS.ANNOUNCEMENTS.{key.ToUpperInvariant()}";

        var category = ModSettingsRegistry.EnsureCategory(
            $"announcements.{key}",
            $"Announcements/{displayName}",
            $"{RootLocKey}/{categoryLocKey}");

        if (category.GetByKey("enabled") == null)
            category.Add(new BoolSetting("enabled", "Announce", true, localizationKey: EnabledLocKey));

        var method = announcementType.GetMethod("RegisterSettings",
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(CategorySetting) }, null);
        method?.Invoke(null, new object[] { category });
    }

    private static void RegisterElementOverrides(Type elementType)
    {
        var orderAttr = elementType.GetCustomAttribute<AnnouncementOrderAttribute>();
        if (orderAttr == null) return;

        var elementKey = DeriveElementKey(elementType);
        var elementDisplay = DeriveDisplayName(StripSuffix(StripSuffix(elementType.Name, "Element"), "Proxy", prefixInstead: true));

        // The per-element "Announcements" subcategory that contains all the
        // announcement overrides — we tag it with HasResetAction so the
        // settings screen shows a "Reset to defaults" button.
        var announcementsParent = ModSettingsRegistry.EnsureCategory(
            $"ui.{elementKey}.announcements",
            $"UI/{elementDisplay}/Announcements",
            $"/SETTINGS.ELEMENTS.{elementKey.ToUpperInvariant()}/{RootLocKey}");
        announcementsParent.HasResetAction = true;

        foreach (var announcementType in orderAttr.Types.Distinct())
        {
            var announcementKey = DeriveAnnouncementKey(announcementType);
            var announcementDisplay = DeriveDisplayName(StripSuffix(announcementType.Name, "Announcement"));
            var announcementCategoryLocKey = $"SETTINGS.ANNOUNCEMENTS.{announcementKey.ToUpperInvariant()}";

            var announcementCategory = ModSettingsRegistry.EnsureCategory(
                $"ui.{elementKey}.announcements.{announcementKey}",
                $"UI/{elementDisplay}/Announcements/{announcementDisplay}",
                $"/SETTINGS.ELEMENTS.{elementKey.ToUpperInvariant()}/{RootLocKey}/{announcementCategoryLocKey}");

            if (announcementCategory.GetByKey("enabled") != null) continue;

            var fallback = ModSettings.GetSetting<BoolSetting>($"announcements.{announcementKey}.enabled");
            if (fallback == null) continue;

            announcementCategory.Add(new NullableBoolSetting(
                "enabled", "Announce", fallback, localizationKey: EnabledLocKey));
        }
    }

    /// <summary>Converts e.g. <c>MonsterIntentsAnnouncement</c> to <c>monster_intents</c>.</summary>
    public static string DeriveAnnouncementKey(Type announcementType) =>
        ToSnakeCase(StripSuffix(announcementType.Name, "Announcement"));

    /// <summary>Converts e.g. <c>ProxyCreature</c> to <c>creature</c>, <c>ButtonElement</c> to <c>button_element</c>.</summary>
    public static string DeriveElementKey(Type elementType) =>
        ToSnakeCase(StripSuffix(StripSuffix(elementType.Name, "Element"), "Proxy", prefixInstead: true));

    private static string DeriveDisplayName(string pascalCase)
    {
        var sb = new StringBuilder(pascalCase.Length + 4);
        for (int i = 0; i < pascalCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascalCase[i]))
                sb.Append(' ');
            sb.Append(pascalCase[i]);
        }
        return sb.ToString();
    }

    private static string ToSnakeCase(string pascalCase)
    {
        var sb = new StringBuilder(pascalCase.Length + 4);
        for (int i = 0; i < pascalCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascalCase[i]))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(pascalCase[i]));
        }
        return sb.ToString();
    }

    private static string StripSuffix(string name, string suffix, bool prefixInstead = false)
    {
        if (prefixInstead)
            return name.StartsWith(suffix) ? name[suffix.Length..] : name;
        return name.EndsWith(suffix) ? name[..^suffix.Length] : name;
    }
}
