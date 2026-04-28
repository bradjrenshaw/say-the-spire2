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

        // Announcements that aren't opted in via [ShowInGlobalSettings] still
        // get their global category (per-element NullableBool overrides need
        // it as a fallback), but it's hidden from the settings UI so the user
        // only sees it via the per-element reorder/configure screens.
        category.Hidden = announcementType.GetCustomAttribute<ShowInGlobalSettingsAttribute>() == null;

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
        var elementDisplay = DeriveElementDisplayName(elementType);

        // The per-element "Announcements" subcategory that contains all the
        // announcement overrides — we tag it with HasResetAction so the
        // settings screen shows a "Reset to defaults" button.
        var announcementsParent = ModSettingsRegistry.EnsureCategory(
            $"ui.{elementKey}.announcements",
            $"UI/{elementDisplay}/Announcements",
            $"/SETTINGS.ELEMENTS.{elementKey.ToUpperInvariant()}/{RootLocKey}");
        announcementsParent.HasResetAction = true;

        // PositionAnnouncement is injected universally by UIElement.GetFocusMessage
        // for any element that has a parent container — it's not part of any
        // [AnnouncementOrder] but every element needs an override entry for it.
        var announcementTypes = orderAttr.Types.Append(typeof(PositionAnnouncement)).Distinct().ToList();

        // Hidden user-order setting — comma-separated announcement keys in
        // whatever order the user has picked. Default matches the attribute
        // order; AnnouncementComposer consults it, falling back to the
        // attribute when an announcement key isn't listed (e.g., we added a
        // new announcement type since the user last saved).
        //
        // When the value changes (either via Move Up / Move Down buttons or
        // via disk load on startup), mirror it onto each sibling category's
        // SortPriority so the settings screen renders in the user's order.
        var orderSetting = announcementsParent.GetByKey("order") as StringSetting;
        if (orderSetting == null)
        {
            var defaultOrder = string.Join(",", announcementTypes.Select(DeriveAnnouncementKey));
            orderSetting = new StringSetting("order", "Announcement Order", defaultOrder) { Hidden = true };
            announcementsParent.Add(orderSetting);
        }
        orderSetting.Changed += newOrder => ApplyOrderToSortPriorities(announcementsParent, newOrder, announcementTypes);
        // Apply the saved CSV's order onto SortPriority eagerly so the initial
        // load reflects it. Without this, a CSV that's missing newly-added
        // announcements (mod update after the user last saved their order)
        // leaves those entries at their registration-time priority, colliding
        // with whatever CSV-listed entry got that index.
        ApplyOrderToSortPriorities(announcementsParent, orderSetting.Value, announcementTypes);

        for (int i = 0; i < announcementTypes.Count; i++)
        {
            var announcementType = announcementTypes[i];
            var announcementKey = DeriveAnnouncementKey(announcementType);
            var announcementDisplay = DeriveDisplayName(StripSuffix(announcementType.Name, "Announcement"));
            var announcementCategoryLocKey = $"SETTINGS.ANNOUNCEMENTS.{announcementKey.ToUpperInvariant()}";

            var announcementCategory = ModSettingsRegistry.EnsureCategory(
                $"ui.{elementKey}.announcements.{announcementKey}",
                $"UI/{elementDisplay}/Announcements/{announcementDisplay}",
                $"/SETTINGS.ELEMENTS.{elementKey.ToUpperInvariant()}/{RootLocKey}/{announcementCategoryLocKey}");
            announcementCategory.SortPriority = i;

            // Mirror every setting declared on the global announcement category
            // as a per-element Nullable* override. Covers Bool, Int, String, Choice
            // — any setting type an announcement's RegisterSettings might declare.
            var globalCategory = ModSettings.GetSetting<CategorySetting>($"announcements.{announcementKey}");
            if (globalCategory == null) continue;

            foreach (var globalChild in globalCategory.Children)
            {
                if (announcementCategory.GetByKey(globalChild.Key) != null)
                    continue;

                var overrideSetting = CreateOverride(globalChild);
                if (overrideSetting != null)
                    announcementCategory.Add(overrideSetting);
            }
        }
    }

    /// <summary>
    /// Rewrites <see cref="Setting.SortPriority"/> on each CategorySetting
    /// child of the announcements parent to match its position in the user's
    /// saved order. Any announcement type not in the saved CSV (e.g. a new
    /// one added by a mod update after the user last saved) is slotted at
    /// its relative attribute-order position via
    /// <see cref="MergeUserOrderWithAttrOrder"/>, not appended at the end.
    /// </summary>
    private static void ApplyOrderToSortPriorities(CategorySetting announcementsParent, string orderCsv,
        IReadOnlyList<Type> attrOrder)
    {
        var merged = MergeUserOrderWithAttrOrder(orderCsv, attrOrder);
        for (int i = 0; i < merged.Count; i++)
        {
            var key = DeriveAnnouncementKey(merged[i]);
            if (announcementsParent.GetByKey(key) is CategorySetting cat)
                cat.SortPriority = i;
        }
    }

    /// <summary>
    /// Combines a user-saved announcement-order CSV with the canonical
    /// attribute order. Walks the CSV first (preserving the user's chosen
    /// sequence for entries they've reordered), then inserts each remaining
    /// attribute-order entry immediately after the nearest preceding
    /// attribute-order neighbor that's already in the result. New
    /// announcements added in a mod update land at their intended slot
    /// instead of being appended at the end.
    /// </summary>
    public static List<Type> MergeUserOrderWithAttrOrder(string? orderCsv, IReadOnlyList<Type> attrOrder)
    {
        var keyToType = new Dictionary<string, Type>();
        foreach (var t in attrOrder)
            keyToType[DeriveAnnouncementKey(t)] = t;

        var result = new List<Type>(attrOrder.Count);
        var seen = new HashSet<Type>();

        if (!string.IsNullOrWhiteSpace(orderCsv))
        {
            foreach (var rawKey in orderCsv.Split(','))
            {
                var key = rawKey.Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (keyToType.TryGetValue(key, out var t) && seen.Add(t))
                    result.Add(t);
            }
        }

        for (int i = 0; i < attrOrder.Count; i++)
        {
            var t = attrOrder[i];
            if (seen.Contains(t)) continue;

            int insertAt = 0;
            for (int j = i - 1; j >= 0; j--)
            {
                var neighbor = attrOrder[j];
                if (seen.Contains(neighbor))
                {
                    insertAt = result.IndexOf(neighbor) + 1;
                    break;
                }
            }

            result.Insert(insertAt, t);
            seen.Add(t);
        }

        return result;
    }

    /// <summary>
    /// Produces the Nullable* override setting that inherits from the given
    /// global setting. Returns null for setting types we don't mirror (e.g.,
    /// BindingSetting, CategorySetting — not relevant to announcement overrides).
    /// </summary>
    private static Setting? CreateOverride(Setting global)
    {
        return global switch
        {
            BoolSetting b => new NullableBoolSetting(b.Key, b.Label, b, localizationKey: b.LocalizationKey),
            IntSetting i => new NullableIntSetting(i.Key, i.Label, i, localizationKey: i.LocalizationKey),
            StringSetting s => new NullableStringSetting(s.Key, s.Label, s, localizationKey: s.LocalizationKey),
            ChoiceSetting c => new NullableChoiceSetting(c.Key, c.Label, c, localizationKey: c.LocalizationKey),
            _ => null,
        };
    }

    /// <summary>Converts e.g. <c>MonsterIntentsAnnouncement</c> to <c>monster_intents</c>.</summary>
    public static string DeriveAnnouncementKey(Type announcementType) =>
        ToSnakeCase(StripSuffix(announcementType.Name, "Announcement"));

    /// <summary>
    /// Converts e.g. <c>ProxyCreature</c> to <c>creature</c>, <c>ButtonElement</c> to <c>button_element</c>.
    /// Honors <see cref="ElementSettingsKeyAttribute"/> when present.
    /// </summary>
    public static string DeriveElementKey(Type elementType)
    {
        var attr = elementType.GetCustomAttribute<ElementSettingsKeyAttribute>();
        if (attr != null)
            return attr.Key;
        return ToSnakeCase(StripSuffix(StripSuffix(elementType.Name, "Element"), "Proxy", prefixInstead: true));
    }

    /// <summary>
    /// Picks the display name for the per-element settings category. When the
    /// class carries <see cref="ElementSettingsKeyAttribute"/> we derive the
    /// label from the key (e.g. "potion" → "Potion", "shop_item" → "Shop
    /// Item") so it matches what the user is actually configuring, instead of
    /// the class name (which would read as "Potion Holder", "Merchant Slot").
    /// </summary>
    private static string DeriveElementDisplayName(Type elementType)
    {
        var attr = elementType.GetCustomAttribute<ElementSettingsKeyAttribute>();
        if (attr != null)
            return SnakeToTitleCase(attr.Key);
        return DeriveDisplayName(StripSuffix(StripSuffix(elementType.Name, "Element"), "Proxy", prefixInstead: true));
    }

    private static string SnakeToTitleCase(string snake)
    {
        var parts = snake.Split('_');
        var sb = new StringBuilder(snake.Length + parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(char.ToUpperInvariant(parts[i][0]));
            sb.Append(parts[i].AsSpan(1));
        }
        return sb.ToString();
    }

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
