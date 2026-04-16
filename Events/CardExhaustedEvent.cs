using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_exhausted", "Card Exhausted", category: "Cards")]
public class CardExhaustedEvent : GameEvent
{
    private readonly string _cardName;

    public CardExhaustedEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_EXHAUSTED", new { card = _cardName });
}
