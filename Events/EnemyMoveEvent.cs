using MegaCrit.Sts2.Core.Entities.Creatures;

namespace SayTheSpire2.Events;

public class EnemyMoveEvent : GameEvent
{
    private readonly string _creatureName;

    public EnemyMoveEvent(Creature creature)
    {
        _creatureName = creature.Name;
    }

    public override string? GetMessage() => _creatureName;
}
