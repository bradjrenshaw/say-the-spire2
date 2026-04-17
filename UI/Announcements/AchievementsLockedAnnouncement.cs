using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Flag announcement: this run had achievements locked. Stateless — yielded
/// only when the game's lock indicator is visible.
/// </summary>
public sealed class AchievementsLockedAnnouncement : Announcement
{
    public override string Key => "achievements_locked";
    public override string Suffix => ",";
    public override Message Render() =>
        Message.Localized("ui", "RUN_HISTORY.ACHIEVEMENTS_LOCKED");
}
