using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The game recolors a creature's health bar when poison or doom will kill
/// it before its next turn, so sighted players don't have to do the math.
/// Speaks the same fact right after the creature's name: "dying to {power}",
/// using the game's own localized power title.
/// </summary>
[ShowInGlobalSettings]
public sealed class DyingAnnouncement : Announcement
{
    private readonly string _powerTitle;

    public DyingAnnouncement(string powerTitle) { _powerTitle = powerTitle; }

    public override string Key => "dying";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "CREATURE.DYING_TO", new { power = _powerTitle });
}
