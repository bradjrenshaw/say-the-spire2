using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_stolen", "Card Stolen")]
public class CardStolenEvent : GameEvent
{
    private readonly string _cardName;

    public CardStolenEvent(string cardName)
    {
        _cardName = cardName;
    }

    public override string? GetMessage()
    {
        if (string.IsNullOrEmpty(_cardName)) return null;
        return $"{_cardName} stolen";
    }
}
