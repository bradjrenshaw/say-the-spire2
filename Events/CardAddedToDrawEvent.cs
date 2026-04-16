using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_added_to_draw", "Card Added to Draw", category: "Cards")]
public class CardAddedToDrawEvent : GameEvent
{
    private readonly string _cardName;

    public CardAddedToDrawEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_ADDED_TO_DRAW", new { card = _cardName });
}
