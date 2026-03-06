using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Events;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyEventOptionButton : ProxyElement
{
    public ProxyEventOptionButton(Control control) : base(control) { }

    private NEventOptionButton? Button => Control as NEventOptionButton;

    public override string? GetLabel()
    {
        var option = Button?.Option;
        if (option == null) return CleanNodeName(Control.Name);

        var title = option.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(title))
            return StripBbcode(title);

        var desc = option.Description?.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            return StripBbcode(desc);

        return CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        var option = Button?.Option;
        if (option == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        var desc = option.Description?.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            parts.Add(StripBbcode(desc));

        if (option.IsLocked)
            parts.Add("Locked");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var option = Button?.Option;
        if (option == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            var title = option.Title?.GetFormattedText();
            if (!string.IsNullOrEmpty(title))
                uiBuffer.Add(StripBbcode(title));

            var desc = option.Description?.GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                uiBuffer.Add(StripBbcode(desc));

            if (option.IsLocked)
                uiBuffer.Add("Locked");

            if (option.Relic != null)
                uiBuffer.Add($"Relic: {option.Relic.Title.GetFormattedText()}");

            // Hover tips (enchantments, keywords, cards, etc.)
            try
            {
                var cardTips = new List<CardHoverTip>();
                foreach (var tip in option.HoverTips)
                {
                    if (tip is CardHoverTip cardTip)
                    {
                        cardTips.Add(cardTip);
                    }
                    else if (tip is HoverTip hoverTip)
                    {
                        var tipTitle = hoverTip.Title;
                        var tipDesc = hoverTip.Description;
                        if (!string.IsNullOrEmpty(tipTitle) && !string.IsNullOrEmpty(tipDesc))
                            uiBuffer.Add($"{tipTitle}: {StripBbcode(tipDesc)}");
                        else if (!string.IsNullOrEmpty(tipTitle))
                            uiBuffer.Add(tipTitle);
                        else if (!string.IsNullOrEmpty(tipDesc))
                            uiBuffer.Add(StripBbcode(tipDesc));
                    }
                }

                if (cardTips.Count > 0)
                {
                    var cardBuffer = buffers.GetBuffer("card");
                    if (cardBuffer != null)
                    {
                        cardBuffer.Clear();
                        foreach (var cardTip in cardTips)
                        {
                            if (cardBuffer.Count > 0)
                                cardBuffer.Add("---");
                            ProxyCard.PopulateCardBuffer(cardBuffer, cardTip.Card);
                        }
                        buffers.EnableBuffer("card", true);
                    }
                }
            }
            catch { }

            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
