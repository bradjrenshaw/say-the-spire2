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

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_damage", "Announce Damage", true));
        category.Add(new BoolSetting("announce_heals", "Announce Heals", true));
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

    public override bool ShouldAnnounce()
    {
        int delta = _newHp - _oldHp;
        if (delta < 0)
            return ModSettings.GetValue<bool>("events.hp.announce_damage");
        if (delta > 0)
            return ModSettings.GetValue<bool>("events.hp.announce_heals");
        return true;
    }
}
