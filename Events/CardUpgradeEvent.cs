using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_upgrade", "Card Upgrade", hasSourceFilter: true, allowEnemies: false)]
public class CardUpgradeEvent : GameEvent
{
    private readonly string _cardName;
    private readonly string? _playerName;
    private readonly bool _isDowngrade;

    public CardUpgradeEvent(string cardName, Creature? source = null, bool isDowngrade = false,
        string? playerName = null)
    {
        Source = source;
        _cardName = cardName;
        _isDowngrade = isDowngrade;
        _playerName = playerName;
    }

    public override Message? GetMessage()
    {
        var key = _isDowngrade ? "EVENT.CARD_DOWNGRADE" : "EVENT.CARD_UPGRADE";

        if (_playerName != null && MultiplayerHelper.IsMultiplayer())
            return Message.Localized("ui", key + "_PLAYER", new { player = _playerName, card = _cardName });

        return Message.Localized("ui", key, new { card = _cardName });
    }
}
