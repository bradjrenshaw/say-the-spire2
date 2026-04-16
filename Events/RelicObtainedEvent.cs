using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("relic_obtained", "Relic Obtained", category: "Resources", hasSourceFilter: true, allowEnemies: false)]
public class RelicObtainedEvent : GameEvent
{
    private readonly string _relicName;

    public RelicObtainedEvent(string relicName, Creature? source = null)
    {
        Source = source;
        _relicName = relicName;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.RELIC_OBTAINED", new { relic = _relicName });
}
