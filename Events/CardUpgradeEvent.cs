using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_upgrade", "Card Upgrade", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class CardUpgradeEvent : GameEvent
{
    private readonly string _cardName;

    public CardUpgradeEvent(string cardName, Creature? source = null)
    {
        Source = source;
        _cardName = cardName;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.CARD_UPGRADE", new { card = _cardName });
}
