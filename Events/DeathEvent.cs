using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("death", "Death", hasSourceFilter: true)]
public class DeathEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly int? _remainingEnemies;

    public DeathEvent(Creature creature, int? remainingEnemies = null)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _remainingEnemies = remainingEnemies;
    }

    public override Message? GetMessage() => _remainingEnemies.HasValue
        ? Message.Localized("ui", "EVENT.DEATH_ENEMY_REMAINING", new { creature = _creatureName, remaining = _remainingEnemies.Value })
        : Message.Localized("ui", "EVENT.DEATH", new { creature = _creatureName });
}
