using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Announces how many items are currently selected across a selection screen
/// (e.g., "3 cards selected" while picking cards to remove or upgrade). Not
/// tied to this specific element's state — reflects the selection-set size.
/// </summary>
[ShowInGlobalSettings]
public sealed class SelectionCountAnnouncement : Announcement
{
    private readonly int _count;

    public SelectionCountAnnouncement(int count) { _count = count; }

    public override string Key => "selection_count";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "CARD.COUNT_SELECTED", new { count = _count });
}
