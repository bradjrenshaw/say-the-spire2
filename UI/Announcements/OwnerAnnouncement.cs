using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The owning player of a pet creature in multiplayer. Renders only when the
/// focused creature is a pet whose owner is another player — local-player
/// pets and singleplayer creatures don't yield this announcement at all.
/// </summary>
[ShowInGlobalSettings]
public sealed class OwnerAnnouncement : Announcement
{
    private readonly string _ownerName;

    public OwnerAnnouncement(string ownerName) { _ownerName = ownerName; }

    public override string Key => "owner";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "MULTIPLAYER.PET_OWNER", new { owner = _ownerName });
}
