using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The element's position within its parent container (e.g. "3 of 12").
/// Injected by <see cref="UIElement.GetFocusMessage"/> when the parent
/// container's <c>AnnouncePosition</c> is set and the parent produces a
/// non-null position message. No per-element order entry: composer appends
/// it at the end by default, subclasses can declare it in their
/// <c>[AnnouncementOrder]</c> to reorder.
/// </summary>
[ShowInGlobalSettings]
public sealed class PositionAnnouncement : Announcement
{
    private readonly Message _position;

    public PositionAnnouncement(Message position) { _position = position; }

    public override string Key => "position";
    public override Message Render(AnnouncementContext ctx) => _position;
}
