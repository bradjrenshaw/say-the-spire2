using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("surrounded_facing", "Surrounded Facing", category: "Combat",
    hasSourceFilter: true, allowEnemies: false)]
public class SurroundedFacingEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly string _targetName;
    private readonly bool _isCurrentPlayer;

    public SurroundedFacingEvent(Creature creature, Creature target)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _targetName = MultiplayerHelper.GetCreatureName(target);
        _isCurrentPlayer = LocalContext.IsMe(creature);
    }

    public override Message? GetMessage()
    {
        if (_isCurrentPlayer)
            return Message.Localized("ui", "EVENT.SURROUNDED_FACING_LOCAL", new { target = _targetName });

        return Message.Localized("ui", "EVENT.SURROUNDED_FACING_OTHER", new
        {
            creature = _creatureName,
            target = _targetName
        });
    }
}
