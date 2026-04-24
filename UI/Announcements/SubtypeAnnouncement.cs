using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The element's subtype label (e.g., "attack", "skill", "power" for cards).
/// Renders from the TYPES.{key} localization table, same source as TypeAnnouncement.
/// Separate announcement class so it gets its own settings identity.
/// </summary>
[ShowInGlobalSettings]
public sealed class SubtypeAnnouncement : Announcement
{
    private readonly string _subtypeKey;

    public SubtypeAnnouncement(string subtypeKey) { _subtypeKey = subtypeKey; }

    public override string Key => "subtype";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", $"TYPES.{_subtypeKey.ToUpperInvariant()}");
}
