using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Marks an element as currently part of a selection set (e.g., a selected card
/// in a card-grid selection screen). Stateless — the caller only yields this
/// when the element is actually selected.
/// </summary>
[ShowInGlobalSettings]
public sealed class SelectedMarkerAnnouncement : Announcement
{
    public override string Key => "selected_marker";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) => Message.Localized("ui", "CARD.SELECTED");
}
