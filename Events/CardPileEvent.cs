using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
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

[EventSettings("card_pile", "Card Pile", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class CardPileEvent : GameEvent
{
    private readonly CardPileEventType _type;
    private readonly string? _cardName;

    public CardPileEvent(CardPileEventType type, string? cardName = null, Creature? source = null)
    {
        Source = source;
        _type = type;
        _cardName = cardName;
    }

    public static void RegisterSettings(Settings.CategorySetting category)
    {
        category.Add(new Settings.BoolSetting("announce_hand_discarded", "Announce Hand Discarded", true));
        category.Add(new Settings.BoolSetting("announce_deck_shuffled", "Announce Deck Shuffled", true));
    }

    public override Message? GetMessage() => _type switch
    {
        CardPileEventType.Drew => Message.Raw($"Drew {_cardName}"),
        CardPileEventType.Discarded => Message.Raw($"{_cardName} added to discard"),
        CardPileEventType.Exhausted => Message.Raw($"{_cardName} exhausted"),
        CardPileEventType.AddedToDraw => Message.Raw($"{_cardName} added to draw"),
        CardPileEventType.HandDiscarded => Message.Raw("Hand discarded"),
        CardPileEventType.DeckShuffled => Message.Raw("Deck shuffled"),
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
