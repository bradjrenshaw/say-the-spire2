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
        CardPileEventType.Drew => Message.Localized("ui", "EVENT.CARD_DRAWN", new { card = _cardName }),
        CardPileEventType.Discarded => Message.Localized("ui", "EVENT.CARD_DISCARDED", new { card = _cardName }),
        CardPileEventType.Exhausted => Message.Localized("ui", "EVENT.CARD_EXHAUSTED", new { card = _cardName }),
        CardPileEventType.AddedToDraw => Message.Localized("ui", "EVENT.CARD_ADDED_TO_DRAW", new { card = _cardName }),
        CardPileEventType.HandDiscarded => Message.Localized("ui", "EVENT.HAND_DISCARDED"),
        CardPileEventType.DeckShuffled => Message.Localized("ui", "EVENT.CARDS_SHUFFLED"),
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
