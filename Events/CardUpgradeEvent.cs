using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_upgrade", "Card Upgrade", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class CardUpgradeEvent : GameEvent
{
    private readonly string _cardName;
    private readonly bool _isDowngrade;

    public CardUpgradeEvent(string cardName, Creature? source = null, bool isDowngrade = false)
    {
        Source = source;
        _cardName = cardName;
        _isDowngrade = isDowngrade;
    }

    public override Message? GetMessage() => Message.Localized("ui",
        _isDowngrade ? "EVENT.CARD_DOWNGRADE" : "EVENT.CARD_UPGRADE",
        new { card = _cardName });
}
