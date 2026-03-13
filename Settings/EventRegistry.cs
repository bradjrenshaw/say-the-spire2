using System;
using System.Collections.Generic;
using SayTheSpire2.Events;

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

        // Event-specific: add standard announce + buffer settings
        if (cat.GetByKey("announce") == null)
            cat.Add(new BoolSetting("announce", "Announce", attr.DefaultAnnounce));
        if (cat.GetByKey("buffer") == null)
            cat.Add(new BoolSetting("buffer", "Add to buffer", attr.DefaultBuffer));
    }

    public static bool ShouldAnnounce(string eventKey)
    {
        return ModSettings.GetValue<bool>($"events.{eventKey}.announce");
    }

    public static bool ShouldBuffer(string eventKey)
    {
        return ModSettings.GetValue<bool>($"events.{eventKey}.buffer");
    }

    public static void RegisterDefaults()
    {
        Register(typeof(BlockEvent));
        Register(typeof(CardPileEvent));
        Register(typeof(CardStolenEvent));
        Register(typeof(GoldEvent));
        Register(typeof(DeathEvent));
        Register(typeof(DialogueEvent));
        Register(typeof(EnemyMoveEvent));
        Register(typeof(HpEvent));
        Register(typeof(PowerEvent));
        Register(typeof(TurnEvent));
        Register(typeof(CardUpgradeEvent));
        Register(typeof(CardObtainedEvent));
        Register(typeof(RelicObtainedEvent));
        Register(typeof(PotionObtainedEvent));
        Register(typeof(OrbEvent));
        Register(typeof(RoomEnteredEvent));
        Register(typeof(MapVoteEvent));
        Register(typeof(EventVoteEvent));
    }
}
