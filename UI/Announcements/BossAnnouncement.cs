using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Hotkey-context announcement of the current act's boss, or both bosses
/// when two are surfaced. Construct with the resolved boss name(s); the
/// caller (RunScreen) owns the run-state logic that decides whether one or
/// two bosses are relevant.
///
/// <para>Honors an "include_prefix" setting (default true): when off, just
/// the boss name(s) are read ("Name" / "Name1 and Name2"); when on, the
/// "Boss" prefix is included ("Boss Name" / "Boss Name1 and Name2").</para>
/// </summary>
public sealed class BossAnnouncement : Announcement
{
    private readonly string _name1;
    private readonly string? _name2;

    public BossAnnouncement(string name1, string? name2 = null)
    {
        _name1 = name1;
        _name2 = name2;
    }

    public override string Key => "boss";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("include_prefix", "Include Prefix", true,
            localizationKey: "SETTINGS.INCLUDE_PREFIX"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var prefix = ctx.ResolveBool(Key, "include_prefix", true);
        bool dual = !string.IsNullOrEmpty(_name2);

        if (prefix)
        {
            return dual
                ? Message.Localized("ui", "TOPBAR.BOSS_DUAL", new { name1 = _name1, name2 = _name2 })
                : Message.Localized("ui", "TOPBAR.BOSS", new { name = _name1 });
        }

        return dual
            ? Message.Localized("ui", "TOPBAR.BOSS_DUAL_NO_PREFIX", new { name1 = _name1, name2 = _name2 })
            : Message.Raw(_name1);
    }
}
