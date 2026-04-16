using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("potion_obtained", "Potion Obtained", category: "Resources", hasSourceFilter: true, allowEnemies: false)]
public class PotionObtainedEvent : GameEvent
{
    private readonly string _potionName;

    public PotionObtainedEvent(string potionName, Creature? source = null)
    {
        Source = source;
        _potionName = potionName;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.POTION_OBTAINED", new { potion = _potionName });
}
