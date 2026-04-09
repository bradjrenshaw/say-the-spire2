using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("block", "Block", hasSourceFilter: true)]
public class BlockEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly int _oldBlock;
    private readonly int _newBlock;

    public BlockEvent(Creature creature, int oldBlock, int newBlock)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _oldBlock = oldBlock;
        _newBlock = newBlock;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_gained", "Announce Block Gained", true));
        category.Add(new BoolSetting("announce_lost", "Announce Block Lost", true));
        category.Add(new BoolSetting("verbose_totals", "Include Block Totals", true));
    }

    public override Message? GetMessage()
    {
        int delta = _newBlock - _oldBlock;
        bool verbose = ModSettings.GetValue<bool>("events.block.verbose_totals");
        if (delta > 0)
            return verbose
                ? Message.Localized("ui", "EVENT.BLOCK_GAINED", new { creature = _creatureName, amount = delta, total = _newBlock })
                : Message.Localized("ui", "EVENT.BLOCK_GAINED_NO_TOTAL", new { creature = _creatureName, amount = delta });
        if (delta < 0 && _newBlock == 0)
            return Message.Localized("ui", "EVENT.BLOCK_LOST_ALL", new { creature = _creatureName });
        if (delta < 0)
            return verbose
                ? Message.Localized("ui", "EVENT.BLOCK_LOST", new { creature = _creatureName, amount = -delta, remaining = _newBlock })
                : Message.Localized("ui", "EVENT.BLOCK_LOST_NO_TOTAL", new { creature = _creatureName, amount = -delta });
        return null;
    }

    public override bool ShouldAnnounce()
    {
        int delta = _newBlock - _oldBlock;
        if (delta > 0)
            return ModSettings.GetValue<bool>("events.block.announce_gained");
        if (delta < 0)
            return ModSettings.GetValue<bool>("events.block.announce_lost");
        return true;
    }
}
