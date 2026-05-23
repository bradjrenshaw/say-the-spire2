using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// "Facing X, Y, Z" — which creatures the player is currently surrounded by
/// and facing. Empty when no surrounding context applies.
/// </summary>
public sealed class FacingAnnouncement : Announcement
{
    private readonly string? _targets;

    public FacingAnnouncement(string? targets)
    {
        _targets = string.IsNullOrEmpty(targets) ? null : targets;
    }

    public override string Key => "facing";
    public override Message Render(AnnouncementContext ctx) =>
        _targets == null ? Message.Empty
            : Message.Localized("ui", "CREATURE.FACING", new { targets = _targets });
}
