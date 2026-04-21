using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// An orb's passive and evoke values, rendered as a single unit. Honors a
/// "verbose" setting that cascades from global default through per-element
/// override:
/// - Verbose: "Passive 3, Evoke 5"
/// - Compact: "3/5"
/// </summary>
public sealed class OrbNumbersAnnouncement : Announcement
{
    private readonly int _passive;
    private readonly int _evoke;

    public OrbNumbersAnnouncement(int passive, int evoke)
    {
        _passive = passive;
        _evoke = evoke;
    }

    public override string Key => "orb_numbers";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        return verbose
            ? Message.Localized("ui", "ORB.NUMBERS_VERBOSE", new { passive = _passive, evoke = _evoke })
            : Message.Localized("ui", "ORB.NUMBERS_COMPACT", new { passive = _passive, evoke = _evoke });
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }
}
