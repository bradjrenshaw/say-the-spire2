using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context grouped announcement for a creature/player's powers. One
/// line per power, with the power's first hover-tip description appended
/// inline ("Strength 2: Add N to attack damage") and any additional hover
/// tips (keyword or card) emitted as their own lines underneath. Matches
/// the rendering that previously lived in <c>PlayerBuffer.AddPowerToBuffer</c>.
/// </summary>
public sealed class PowersAnnouncement : Announcement
{
    private readonly IReadOnlyList<PowerModel> _powers;

    public PowersAnnouncement(IEnumerable<PowerModel> powers)
    {
        _powers = new List<PowerModel>(powers);
    }

    public override string Key => "powers";

    /// <summary>Focus context: powers are surfaced separately, so the focus line is empty.</summary>
    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var power in _powers)
        {
            foreach (var line in RenderPower(power))
                yield return line;
        }
    }

    private static IEnumerable<Message> RenderPower(PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        var amount = power.DisplayAmount;
        var hasStacks = power.StackType == PowerStackType.Counter;
        var line = hasStacks && amount > 0 ? $"{title} {amount}" : title;

        bool first = true;
        IEnumerable<IHoverTip>? tips = null;
        try { tips = power.HoverTips; }
        catch (Exception e) { Log.Info($"[AccessibilityMod] Power hover tip lookup failed: {e.Message}"); }

        if (tips == null)
        {
            yield return Message.Raw(line);
            yield break;
        }

        foreach (var tip in tips)
        {
            if (tip is HoverTip ht)
            {
                var desc = ht.Description;
                if (first)
                {
                    if (!string.IsNullOrEmpty(desc))
                        line += ": " + desc;
                    yield return Message.Raw(line);
                    first = false;
                }
                else
                {
                    var extraTitle = ht.Title;
                    var extraLine = !string.IsNullOrEmpty(extraTitle) && !string.IsNullOrEmpty(desc)
                        ? $"{extraTitle}: {desc}"
                        : !string.IsNullOrEmpty(extraTitle) ? extraTitle
                        : desc;
                    if (!string.IsNullOrEmpty(extraLine))
                        yield return Message.Raw(extraLine);
                }
            }
            else if (tip is CardHoverTip cardTip)
            {
                if (first)
                {
                    yield return Message.Raw(line);
                    first = false;
                }
                if (cardTip.Card != null)
                {
                    var formatted = CardBuffer.FormatHoverTip(cardTip.Card);
                    if (!string.IsNullOrEmpty(formatted))
                        yield return Message.Raw(formatted);
                }
            }
        }
        if (first)
            yield return Message.Raw(line);
    }
}
