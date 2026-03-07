using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

public enum PowerEventType { Applied, Increased, Decreased, Removed }

[EventSettings("power", "Powers")]
public class PowerEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly string _powerName;
    private readonly int _amount;
    private readonly PowerEventType _eventType;

    public PowerEvent(Creature creature, PowerModel power, PowerEventType eventType, int amount = 0)
    {
        _creatureName = creature.Name;
        _powerName = power.Title.GetFormattedText();
        _amount = amount;
        _eventType = eventType;
    }

    public override string? GetMessage()
    {
        return _eventType switch
        {
            PowerEventType.Increased => $"{_creatureName} gained {_amount} {_powerName}",
            PowerEventType.Decreased => $"{_creatureName} {_powerName} decreased",
            PowerEventType.Removed => $"{_creatureName} lost {_powerName}",
            _ => null
        };
    }
}
