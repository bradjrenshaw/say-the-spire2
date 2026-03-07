using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("hp", "HP Changes")]
public class HpEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly int _oldHp;
    private readonly int _newHp;

    public HpEvent(Creature creature, int oldHp, int newHp)
    {
        _creatureName = creature.Name;
        _oldHp = oldHp;
        _newHp = newHp;
    }

    public override string? GetMessage()
    {
        int delta = _newHp - _oldHp;
        if (delta < 0)
            return $"{_creatureName} {-delta} damage";
        if (delta > 0)
            return $"{_creatureName} healed {delta}";
        return null;
    }
}
