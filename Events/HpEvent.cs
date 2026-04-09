using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("hp", "HP Changes", hasSourceFilter: true)]
public class HpEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly int _oldHp;
    private readonly int _newHp;

    public HpEvent(Creature creature, int oldHp, int newHp)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _oldHp = oldHp;
        _newHp = newHp;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_damage", "Announce Damage", true));
        category.Add(new BoolSetting("announce_heals", "Announce Heals", true));
    }

    public override Message? GetMessage()
    {
        int delta = _newHp - _oldHp;
        if (delta < 0)
            return Message.Localized("ui", "EVENT.HP_DAMAGE", new { creature = _creatureName, amount = -delta });
        if (delta > 0)
            return Message.Localized("ui", "EVENT.HP_HEALED", new { creature = _creatureName, amount = delta });
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
