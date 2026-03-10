using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

public enum OrbEventType
{
    Channeled,
    Evoked,
}

[EventSettings("orb", "Orbs")]
public class OrbEvent : GameEvent
{
    private readonly OrbEventType _type;
    private readonly string _orbName;

    public OrbEvent(OrbEventType type, string orbName)
    {
        _type = type;
        _orbName = orbName;
    }

    public static void RegisterSettings(Settings.CategorySetting category)
    {
        category.Add(new Settings.BoolSetting("announce_channeled", "Announce Channeled", true));
        category.Add(new Settings.BoolSetting("announce_evoked", "Announce Evoked", true));
    }

    public override string? GetMessage() => _type switch
    {
        OrbEventType.Channeled => $"Channeled {_orbName}",
        OrbEventType.Evoked => $"Evoked {_orbName}",
        _ => null,
    };

    public override bool ShouldAnnounce() => _type switch
    {
        OrbEventType.Channeled => Settings.ModSettings.GetValue<bool>("events.orb.announce_channeled"),
        OrbEventType.Evoked => Settings.ModSettings.GetValue<bool>("events.orb.announce_evoked"),
        _ => true,
    };
}
