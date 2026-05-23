using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A creature or player's current / max HP. Honors a "verbose" setting
/// (default true) that cascades per-element / per-buffer / global:
/// - Verbose: "12/30 HP"
/// - Compact: "12/30"
/// </summary>
[ShowInGlobalSettings]
public sealed class HpAnnouncement : Announcement
{
    private readonly int _current;
    private readonly int _max;

    public HpAnnouncement(int current, int max)
    {
        _current = current;
        _max = max;
    }

    public override string Key => "hp";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        return verbose
            ? Message.Localized("ui", "RESOURCE.HP", new { current = _current, max = _max })
            : Message.Localized("ui", "RESOURCE.HP_COMPACT", new { current = _current, max = _max });
    }
}
