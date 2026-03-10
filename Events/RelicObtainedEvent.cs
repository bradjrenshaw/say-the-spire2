using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("relic_obtained", "Relic Obtained")]
public class RelicObtainedEvent : GameEvent
{
    private readonly string _relicName;

    public RelicObtainedEvent(string relicName)
    {
        _relicName = relicName;
    }

    public override string? GetMessage() => $"{_relicName} obtained";
}
