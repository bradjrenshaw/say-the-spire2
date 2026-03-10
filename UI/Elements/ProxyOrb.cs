using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyOrb : ProxyElement
{
    public ProxyOrb(Control control) : base(control) { }

    private NOrb? Orb => Control as NOrb;

    public override string? GetLabel()
    {
        var model = Orb?.Model;
        if (model == null)
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            return tip.Title;
        }
        return model.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "orb";

    public override string? GetStatusString()
    {
        var model = Orb?.Model;
        if (model == null)
        {
            var tip = OrbModel.EmptySlotHoverTipHoverTip;
            var desc = tip.Description;
            return !string.IsNullOrEmpty(desc) ? StripBbcode(desc) : null;
        }

        return $"Passive {model.PassiveVal:0}, Evoke {model.EvokeVal:0}";
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

        buffer.Add($"Passive: {model.PassiveVal:0}, Evoke: {model.EvokeVal:0}");

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
        catch { }
    }
}
