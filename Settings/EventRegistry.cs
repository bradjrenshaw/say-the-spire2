using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Settings;

public static class EventRegistry
{
    private static readonly Dictionary<string, EventSettingsAttribute> _descriptors = new();

    public static IReadOnlyDictionary<string, EventSettingsAttribute> Descriptors => _descriptors;

    public static void Register(Type eventType)
    {
        var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(eventType, typeof(EventSettingsAttribute));
        if (attr == null)
            throw new InvalidOperationException($"{eventType.Name} is missing [EventSettings] attribute");

        _descriptors[attr.Key] = attr;

        // Generic system creates/finds the category and calls RegisterSettings
        var cat = ModSettingsRegistry.Register(eventType);

        // Reparent into a visual-only group category if one is specified
        if (attr.Category != null)
            ReparentIntoGroup(cat, attr.Category);

        // Event-specific: add standard announce + buffer settings
        if (cat.GetByKey("announce") == null)
            cat.Add(new BoolSetting("announce", "Announce", attr.DefaultAnnounce, localizationKey: "EVENTS.COMMON.ANNOUNCE"));
        if (cat.GetByKey("buffer") == null)
            cat.Add(new BoolSetting("buffer", "Add to buffer", attr.DefaultBuffer, localizationKey: "EVENTS.COMMON.BUFFER"));

        // Source filter subcategory (for events that apply to a creature)
        if (attr.HasSourceFilter && cat.GetByKey("sources") == null)
        {
            var sources = new CategorySetting("sources", "Sources", localizationKey: "EVENTS.COMMON.SOURCES");
            if (attr.AllowCurrentPlayer)
                sources.Add(new BoolSetting("current_player", "Current Player", attr.DefaultCurrentPlayer, localizationKey: "EVENTS.COMMON.SOURCE_CURRENT_PLAYER"));
            if (attr.AllowOtherPlayers)
                sources.Add(new BoolSetting("other_players", "Other Players", attr.DefaultOtherPlayers, localizationKey: "EVENTS.COMMON.SOURCE_OTHER_PLAYERS"));
            if (attr.AllowEnemies)
                sources.Add(new BoolSetting("enemies", "Enemies", attr.DefaultEnemies, localizationKey: "EVENTS.COMMON.SOURCE_ENEMIES"));
            cat.Add(sources);
        }
    }

    public static bool ShouldAnnounce(string eventKey)
    {
        return ModSettings.GetValue<bool>($"events.{eventKey}.announce");
    }

    public static bool ShouldBuffer(string eventKey)
    {
        return ModSettings.GetValue<bool>($"events.{eventKey}.buffer");
    }

    /// <summary>
    /// Check if the event should be processed based on its source creature
    /// and the user's source filter settings. Returns true if:
    /// - The event has no source (no creature to filter on)
    /// - The event type doesn't have source filtering enabled
    /// - The source creature's category (current player / other player / enemy) is enabled
    /// </summary>
    public static bool PassesSourceFilter(string eventKey, Creature? source)
    {
        if (source == null) return true;
        if (!_descriptors.TryGetValue(eventKey, out var attr) || !attr.HasSourceFilter) return true;

        var basePath = $"events.{eventKey}.sources";

        if (source.IsPlayer)
        {
            bool isMe = LocalContext.IsMe(source);
            if (isMe)
                return attr.AllowCurrentPlayer && ModSettings.GetValue<bool>($"{basePath}.current_player");
            return attr.AllowOtherPlayers && ModSettings.GetValue<bool>($"{basePath}.other_players");
        }

        return attr.AllowEnemies && ModSettings.GetValue<bool>($"{basePath}.enemies");
    }

    public static void RegisterDefaults()
    {
        var eventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<EventSettingsAttribute>() != null);

        foreach (var type in eventTypes)
        {
            try
            {
                Register(type);
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Failed to register event type {type.Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Moves an event's category setting under a visual-only group category.
    /// The group has includeInPath: false so the settings key path is unchanged.
    /// </summary>
    private static void ReparentIntoGroup(CategorySetting eventCat, string groupLabel)
    {
        var eventsParent = eventCat.Parent as CategorySetting;
        if (eventsParent == null) return;

        // Find or create the visual-only group
        var groupKey = groupLabel.ToLowerInvariant().Replace(" ", "_");
        var group = eventsParent.GetByKey(groupKey) as CategorySetting;
        if (group == null)
        {
            var localizedLabel = Localization.LocalizationManager.GetOrDefault("ui",
                $"EVENT_CATEGORIES.{groupKey.ToUpperInvariant()}", groupLabel);
            group = new CategorySetting(groupKey, localizedLabel, includeInPath: false);
            eventsParent.Add(group);
        }

        eventsParent.Remove(eventCat);
        group.Add(eventCat);
    }
}
