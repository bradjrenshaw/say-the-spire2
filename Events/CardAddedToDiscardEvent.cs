using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_added_to_discard", "Card Added to Discard", category: "Cards")]
public class CardAddedToDiscardEvent : GameEvent
{
    private readonly string _cardName;

    public CardAddedToDiscardEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_ADDED_TO_DISCARD", new { card = _cardName });
}
