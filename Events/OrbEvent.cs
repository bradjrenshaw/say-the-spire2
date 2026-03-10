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

    public override string? GetMessage() => _type switch
    {
        OrbEventType.Channeled => $"Channeled {_orbName}",
        OrbEventType.Evoked => $"Evoked {_orbName}",
        _ => null,
    };
}
