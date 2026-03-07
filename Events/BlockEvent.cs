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

    public override string? GetMessage()
    {
        int delta = _newBlock - _oldBlock;
        if (delta > 0)
            return $"{_creatureName} gained {delta} Block ({_newBlock} total)";
        if (delta < 0 && _newBlock > 0)
            return $"{_creatureName} lost {-delta} Block ({_newBlock} remaining)";
        if (delta < 0 && _newBlock == 0)
            return $"{_creatureName} lost all Block";
        return null;
    }
}
