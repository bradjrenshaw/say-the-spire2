using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_discarded", "Card Discarded", category: "Cards")]
public class CardDiscardedEvent : GameEvent
{
    private readonly string _cardName;

    public CardDiscardedEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_DISCARDED", new { card = _cardName });
}
