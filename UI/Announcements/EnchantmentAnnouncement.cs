using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context enchantment block on a card. May yield 1–3 lines: the
/// title+description, the amount (if shown), and a disabled marker (if the
/// enchantment is currently disabled). Construct with all the resolved
/// fields; the announcement just formats them.
/// </summary>
public sealed class EnchantmentAnnouncement : Announcement
{
    private readonly string? _title;
    private readonly string? _description;
    private readonly int? _amount;
    private readonly bool _disabled;

    public EnchantmentAnnouncement(string? title, string? description, int? amount, bool disabled)
    {
        _title = title;
        _description = description;
        _amount = amount;
        _disabled = disabled;
    }

    public override string Key => "enchantment";

    public override Message Render(AnnouncementContext ctx) => FirstLine() ?? Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        var first = FirstLine();
        if (first != null) yield return first;
        if (_amount.HasValue && _amount.Value != 0)
            yield return Message.Localized("ui", "CARD.ENCHANTMENT_AMOUNT", new { amount = _amount.Value });
        if (_disabled)
            yield return Message.Localized("ui", "CARD.ENCHANTMENT_DISABLED");
    }

    private Message? FirstLine()
    {
        if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_description))
            return Message.Localized("ui", "CARD.ENCHANTMENT", new { title = _title, description = _description });
        if (!string.IsNullOrEmpty(_title))
            return Message.Localized("ui", "CARD.ENCHANTMENT_NO_DESC", new { title = _title });
        return null;
    }
}
