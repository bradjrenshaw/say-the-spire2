using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("gold", "Gold")]
public class GoldEvent : GameEvent
{
    private readonly int _oldGold;
    private readonly int _newGold;

    public GoldEvent(int oldGold, int newGold)
    {
        _oldGold = oldGold;
        _newGold = newGold;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_gained", "Announce Gold Gained", true));
        category.Add(new BoolSetting("announce_lost", "Announce Gold Lost", true));
        category.Add(new BoolSetting("verbose_totals", "Include Gold Totals", true));
    }

    public override string? GetMessage()
    {
        int delta = _newGold - _oldGold;
        bool showTotals = ModSettings.GetValue<bool>("events.gold.verbose_totals");
        if (delta > 0)
            return showTotals
                ? $"Gained {delta} gold ({_newGold} total)"
                : $"Gained {delta} gold";
        if (delta < 0)
            return showTotals
                ? $"Lost {-delta} gold ({_newGold} remaining)"
                : $"Lost {-delta} gold";
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
