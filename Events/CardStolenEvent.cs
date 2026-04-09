using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_stolen", "Card Stolen", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class CardStolenEvent : GameEvent
{
    private readonly string _cardName;
    private readonly string _thiefName;

    public CardStolenEvent(string cardName, string thiefName = "", Creature? source = null)
    {
        Source = source;
        _cardName = cardName;
        _thiefName = thiefName;
    }

    public override Message? GetMessage()
    {
        if (string.IsNullOrEmpty(_cardName)) return null;
        return Message.Localized("ui", "EVENT.CARD_STOLEN", new { card = _cardName, thief = _thiefName });
    }
}
