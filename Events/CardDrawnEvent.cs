using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_drawn", "Card Drawn", category: "Cards")]
public class CardDrawnEvent : GameEvent
{
    private readonly string _cardName;

    public CardDrawnEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_DRAWN", new { card = _cardName });
}
