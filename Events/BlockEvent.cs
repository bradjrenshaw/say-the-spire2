using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("block", "Block")]
public class BlockEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly int _oldBlock;
    private readonly int _newBlock;

    public BlockEvent(Creature creature, int oldBlock, int newBlock)
    {
        _creatureName = creature.Name;
        _oldBlock = oldBlock;
        _newBlock = newBlock;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_gained", "Announce Block Gained", true));
        category.Add(new BoolSetting("announce_lost", "Announce Block Lost", true));
        category.Add(new BoolSetting("announce_all_lost", "Announce All Block Lost", true));
        category.Add(new BoolSetting("verbose_totals", "Include Block Totals", true));
    }

    public override string? GetMessage()
    {
        int delta = _newBlock - _oldBlock;
        bool verbose = ModSettings.GetValue<bool>("events.block.verbose_totals");
        if (delta > 0)
            return verbose
                ? $"{_creatureName} gained {delta} Block ({_newBlock} total)"
                : $"{_creatureName} gained {delta} Block";
        if (delta < 0 && _newBlock == 0 && ModSettings.GetValue<bool>("events.block.announce_all_lost"))
            return $"{_creatureName} lost all Block";
        if (delta < 0)
            return verbose
                ? $"{_creatureName} lost {-delta} Block ({_newBlock} remaining)"
                : $"{_creatureName} lost {-delta} Block";
        return null;
    }

    public override bool ShouldAnnounce()
    {
        int delta = _newBlock - _oldBlock;
        if (delta > 0)
            return ModSettings.GetValue<bool>("events.block.announce_gained");
        if (delta < 0 && _newBlock == 0)
            return ModSettings.GetValue<bool>("events.block.announce_all_lost")
                || ModSettings.GetValue<bool>("events.block.announce_lost");
        if (delta < 0)
            return ModSettings.GetValue<bool>("events.block.announce_lost");
        return true;
    }
}
