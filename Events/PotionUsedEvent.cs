using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("potion_used", "Potion Used",
    hasSourceFilter: true, allowEnemies: false,
    defaultCurrentPlayer: false)]
public class PotionUsedEvent : GameEvent
{
    private readonly string _playerName;
    private readonly string _potionName;

    public PotionUsedEvent(string playerName, string potionName, Creature? source = null)
    {
        Source = source;
        _playerName = playerName;
        _potionName = potionName;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.POTION_USED", new { player = _playerName, potion = _potionName });
}
