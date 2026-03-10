using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("potion_obtained", "Potion Obtained")]
public class PotionObtainedEvent : GameEvent
{
    private readonly string _potionName;

    public PotionObtainedEvent(string potionName)
    {
        _potionName = potionName;
    }

    public override string? GetMessage() => $"{_potionName} obtained";
}
