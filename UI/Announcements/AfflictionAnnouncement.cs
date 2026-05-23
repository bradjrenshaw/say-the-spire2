using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context affliction block on a card. May yield 1–2 lines: the
/// title+description and the amount (when the affliction is stackable and
/// has a non-zero stack count). Parallels <see cref="EnchantmentAnnouncement"/>.
/// </summary>
public sealed class AfflictionAnnouncement : Announcement
{
    private readonly string? _title;
    private readonly string? _description;
    private readonly int? _amount;

    public AfflictionAnnouncement(string? title, string? description, int? amount)
    {
        _title = title;
        _description = description;
        _amount = amount;
    }

    public override string Key => "affliction";

    public override Message Render(AnnouncementContext ctx) => FirstLine() ?? Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        var first = FirstLine();
        if (first != null) yield return first;
        if (_amount.HasValue && _amount.Value > 0)
            yield return Message.Localized("ui", "CARD.AFFLICTION_AMOUNT", new { amount = _amount.Value });
    }

    private Message? FirstLine()
    {
        if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_description))
            return Message.Localized("ui", "CARD.AFFLICTION", new { title = _title, description = _description });
        if (!string.IsNullOrEmpty(_title))
            return Message.Localized("ui", "CARD.AFFLICTION_NO_DESC", new { title = _title });
        return null;
    }
}
