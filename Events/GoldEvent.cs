using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("gold", "Gold", category: "Resources", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class GoldEvent : GameEvent
{
    private readonly int _oldGold;
    private readonly int _newGold;

    public GoldEvent(int oldGold, int newGold, Creature? source = null)
    {
        Source = source;
        _oldGold = oldGold;
        _newGold = newGold;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_gained", "Announce Gold Gained", true));
        category.Add(new BoolSetting("announce_lost", "Announce Gold Lost", true));
        category.Add(new BoolSetting("verbose_totals", "Include Gold Totals", true));
    }

    public override Message? GetMessage()
    {
        int delta = _newGold - _oldGold;
        bool showTotals = ModSettings.GetValue<bool>("events.gold.verbose_totals");
        if (delta > 0)
            return showTotals
                ? Message.Localized("ui", "EVENT.GOLD_GAINED", new { amount = delta, total = _newGold })
                : Message.Localized("ui", "EVENT.GOLD_GAINED_NO_TOTAL", new { amount = delta });
        if (delta < 0)
            return showTotals
                ? Message.Localized("ui", "EVENT.GOLD_LOST", new { amount = -delta, remaining = _newGold })
                : Message.Localized("ui", "EVENT.GOLD_LOST_NO_TOTAL", new { amount = -delta });
        return null;
    }

    public override bool ShouldAnnounce()
    {
        int delta = _newGold - _oldGold;
        if (delta > 0)
            return ModSettings.GetValue<bool>("events.gold.announce_gained");
        if (delta < 0)
            return ModSettings.GetValue<bool>("events.gold.announce_lost");
        return true;
    }
}
