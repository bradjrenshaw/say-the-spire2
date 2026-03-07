using System;
using System.Collections.Generic;

namespace SayTheSpire2.Settings;

public static class EventRegistry
{
    private static readonly Dictionary<string, EventSettingsAttribute> _descriptors = new();
    private static CategorySetting? _eventsCategory;

    public static IReadOnlyDictionary<string, EventSettingsAttribute> Descriptors => _descriptors;

    public static void Initialize(CategorySetting eventsCategory)
    {
        _eventsCategory = eventsCategory;
    }

    public static void Register(Type eventType)
    {
        var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(eventType, typeof(EventSettingsAttribute));
        if (attr == null)
            throw new InvalidOperationException($"{eventType.Name} is missing [EventSettings] attribute");

        _descriptors[attr.Key] = attr;

        if (_eventsCategory != null)
        {
            var cat = new CategorySetting(attr.Key, attr.Label);
            cat.Add(new BoolSetting("announce", "Announce", attr.DefaultAnnounce));
            cat.Add(new BoolSetting("buffer", "Add to buffer", attr.DefaultBuffer));
            _eventsCategory.Add(cat);
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
}
