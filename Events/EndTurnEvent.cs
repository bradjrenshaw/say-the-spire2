using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("end_turn", "End Turn",
    category: "Combat",
    hasSourceFilter: true, allowEnemies: false,
    defaultCurrentPlayer: false)]
public class EndTurnEvent : GameEvent
{
    private readonly string _playerName;
    private readonly bool _ready;

    public EndTurnEvent(string playerName, bool ready, Creature? source = null)
    {
        Source = source;
        _playerName = playerName;
        _ready = ready;
    }

    public override Message? GetMessage() =>
        _ready
            ? Message.Localized("ui", "EVENT.END_TURN_READY", new { player = _playerName })
            : Message.Localized("ui", "EVENT.END_TURN_CANCEL", new { player = _playerName });
    public override bool ShouldAddToBuffer() => false;
}
