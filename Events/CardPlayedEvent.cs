using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_played", "Card Played",
    hasSourceFilter: true, allowEnemies: false,
    defaultCurrentPlayer: false)]
public class CardPlayedEvent : GameEvent
{
    private readonly string _playerName;
    private readonly string _cardName;

    public CardPlayedEvent(string playerName, string cardName, Creature? source = null)
    {
        Source = source;
        _playerName = playerName;
        _cardName = cardName;
    }

    public override Message? GetMessage() => Message.Raw($"{_playerName} played {_cardName}");
}
