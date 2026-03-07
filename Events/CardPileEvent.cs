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

public class CardPileEvent : GameEvent
{
    private readonly CardPileEventType _type;
    private readonly string? _cardName;

    public CardPileEvent(CardPileEventType type, string? cardName = null)
    {
        _type = type;
        _cardName = cardName;
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
}
