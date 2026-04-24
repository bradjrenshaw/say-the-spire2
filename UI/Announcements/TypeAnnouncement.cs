using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The element's type label (e.g., "creature", "card"). Renders from the
/// TYPES.{key} localization table.
/// </summary>
[ShowInGlobalSettings]
public sealed class TypeAnnouncement : Announcement
{
    private readonly string _typeKey;

    public TypeAnnouncement(string typeKey) { _typeKey = typeKey; }

    public override string Key => "type";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", $"TYPES.{_typeKey.ToUpperInvariant()}");
}
