using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

public enum PowerEventType { Applied, Increased, Decreased, Removed }

[EventSettings("power", "Powers", category: "Combat", hasSourceFilter: true)]
public class PowerEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly string _powerName;
    private readonly int _amount;
    private readonly PowerEventType _eventType;
    private readonly bool _hasStacks;

    public PowerEvent(Creature creature, PowerModel power, PowerEventType eventType, int amount = 0)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _powerName = power.Title.GetFormattedText();
        _amount = amount;
        _eventType = eventType;
        _hasStacks = power.StackType == PowerStackType.Counter;
    }

    public override Message? GetMessage()
    {
        return _eventType switch
        {
            PowerEventType.Increased when _hasStacks && _amount > 0 => Message.Localized("ui", "EVENT.POWER_GAINED", new { creature = _creatureName, amount = _amount, power = _powerName }),
            PowerEventType.Increased => Message.Localized("ui", "EVENT.POWER_GAINED_NO_AMOUNT", new { creature = _creatureName, power = _powerName }),
            PowerEventType.Decreased => Message.Localized("ui", "EVENT.POWER_DECREASED", new { creature = _creatureName, power = _powerName }),
            PowerEventType.Removed => Message.Localized("ui", "EVENT.POWER_LOST", new { creature = _creatureName, power = _powerName }),
            _ => null
        };
    }
}
