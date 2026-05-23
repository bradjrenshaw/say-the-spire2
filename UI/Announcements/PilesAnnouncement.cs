using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// One-line summary of the player's card pile counts: draw / hand /
/// discard, with exhaust appended when non-zero. Buffer-only — the focus
/// already surfaces CardsInHandAnnouncement separately.
/// </summary>
public sealed class PilesAnnouncement : Announcement
{
    private readonly int _draw;
    private readonly int _hand;
    private readonly int _discard;
    private readonly int _exhaust;

    public PilesAnnouncement(int draw, int hand, int discard, int exhaust)
    {
        _draw = draw;
        _hand = hand;
        _discard = discard;
        _exhaust = exhaust;
    }

    public override string Key => "piles";
    public override Message Render(AnnouncementContext ctx)
    {
        var line = Message.Localized("ui", "RESOURCE.DRAW_HAND_DISCARD",
            new { draw = _draw, hand = _hand, discard = _discard });
        if (_exhaust > 0)
            line = Message.Join(", ", line,
                Message.Localized("ui", "RESOURCE.EXHAUST", new { count = _exhaust }));
        return line;
    }
}
