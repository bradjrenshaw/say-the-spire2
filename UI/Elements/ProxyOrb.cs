using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(OrbNumbersAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyOrb : ProxyElement
{
    public ProxyOrb(Control control) : base(control) { }

    private NOrb? Orb => Control as NOrb;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("orb");

        var model = Orb?.Model;
        if (model != null)
        {
            yield return new OrbNumbersAnnouncement((int)model.PassiveVal, (int)model.EvokeVal);
        }
        else
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            var desc = tip.Description;
            if (!string.IsNullOrEmpty(desc))
                yield return new TooltipAnnouncement(StripBbcode(desc));
        }
    }

    public override Message? GetLabel()
    {
        var model = Orb?.Model;
        if (model == null)
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            var title = tip.Title;
            return title != null ? Message.Raw(title) : null;
        }
        return Message.Raw(model.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "orb";

    public override Message? GetStatusString()
    {
        var model = Orb?.Model;
        if (model == null)
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            var desc = tip.Description;
            return !string.IsNullOrEmpty(desc) ? Message.Raw(StripBbcode(desc)) : null;
        }

        return Message.Localized("ui", "ORB.STATUS", new { passive = (int)model.PassiveVal, evoke = (int)model.EvokeVal });
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var model = Orb?.Model;
        if (model == null)
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            var title = tip.Title;
            if (!string.IsNullOrEmpty(title))
                uiBuffer.Add(title);
            var desc = tip.Description;
            if (!string.IsNullOrEmpty(desc))
                uiBuffer.Add(StripBbcode(desc));
        }
        else
        {
            PopulateOrbBuffer(uiBuffer, model);
        }

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    public static void PopulateOrbBuffer(Buffer buffer, OrbModel model)
    {
        buffer.Add(model.Title.GetFormattedText());

        buffer.Add(Message.Localized("ui", "ORB.BUFFER_STATUS", new { passive = (int)model.PassiveVal, evoke = (int)model.EvokeVal }).Resolve());

        try
        {
            foreach (var tip in model.HoverTips)
            {
                if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var desc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                        buffer.Add($"{title}: {StripBbcode(desc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Orb hover tips access failed: {e.Message}"); }
    }
}
