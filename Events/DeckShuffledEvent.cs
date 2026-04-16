using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("deck_shuffled", "Deck Shuffled", category: "Cards")]
public class DeckShuffledEvent : GameEvent
{
    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARDS_SHUFFLED");
}
