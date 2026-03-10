using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

public enum CardPileEventType
{
    Drew,
    Discarded,
    Exhausted,
    AddedToDraw,
    HandDiscarded,
    DeckShuffled,
}

[EventSettings("card_pile", "Card Pile")]
public class CardPileEvent : GameEvent
{
    private readonly CardPileEventType _type;
    private readonly string? _cardName;

    public CardPileEvent(CardPileEventType type, string? cardName = null)
    {
        _type = type;
        _cardName = cardName;
    }

    public static void RegisterSettings(Settings.CategorySetting category)
    {
        category.Add(new Settings.BoolSetting("announce_hand_discarded", "Announce Hand Discarded", true));
        category.Add(new Settings.BoolSetting("announce_deck_shuffled", "Announce Deck Shuffled", true));
    }

    public override string? GetMessage() => _type switch
    {
        CardPileEventType.Drew => $"Drew {_cardName}",
        CardPileEventType.Discarded => $"{_cardName} added to discard",
        CardPileEventType.Exhausted => $"{_cardName} exhausted",
        CardPileEventType.AddedToDraw => $"{_cardName} added to draw",
        CardPileEventType.HandDiscarded => "Hand discarded",
        CardPileEventType.DeckShuffled => "Deck shuffled",
        _ => null,
    };

    public override bool ShouldAnnounce()
    {
        if (_type == CardPileEventType.HandDiscarded)
            return Settings.ModSettings.GetValue<bool>("events.card_pile.announce_hand_discarded");
        if (_type == CardPileEventType.DeckShuffled)
            return Settings.ModSettings.GetValue<bool>("events.card_pile.announce_deck_shuffled");
        return true;
    }
}
