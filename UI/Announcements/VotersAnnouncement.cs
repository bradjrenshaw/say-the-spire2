using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Lists the co-op players who have voted to travel to this item in multiplayer
/// (e.g., a map node). Renders only when at least one player has voted.
/// </summary>
[ShowInGlobalSettings]
public sealed class VotersAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _voters;

    public VotersAnnouncement(IReadOnlyList<string> voters) { _voters = voters; }

    public override string Key => "voters";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx)
    {
        if (_voters.Count == 0) return Message.Empty;
        return Message.Localized("ui", "EVENT.VOTED_FOR_BY", new
        {
            players = string.Join(", ", _voters)
        });
    }
}
