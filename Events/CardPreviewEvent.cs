using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_preview", "Card Preview", category: "Cards")]
public class CardPreviewEvent : GameEvent
{
    private readonly string _cardName;

    public CardPreviewEvent(string cardName) { _cardName = cardName; }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_PREVIEW", new { card = _cardName });
}
