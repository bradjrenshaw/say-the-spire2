using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyRelicHolder : ProxyElement
{
    public ProxyRelicHolder(Control control) : base(control) { }

    private RelicModel? GetModel()
    {
        if (Control is NRelicInventoryHolder invHolder)
            return invHolder.Relic?.Model;

        if (Control is NTreasureRoomRelicHolder treasureHolder)
            return treasureHolder.Relic?.Model;

        if (Control is NRelicBasicHolder basicHolder)
            return basicHolder.Relic?.Model;

        return null;
    }

    public override string? GetLabel()
    {
        var model = GetModel();
        if (model == null) return CleanNodeName(Control.Name);
        return model.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "relic";

    public override string? GetStatusString()
    {
        var model = GetModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            parts.Add(StripBbcode(desc));

        if (model.ShowCounter && model.DisplayAmount != 0)
            parts.Add($"Counter: {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            parts.Add("Disabled");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetModel();
        if (model == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            PopulateRelicBuffer(uiBuffer, model, buffers);
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }

    public static void PopulateRelicBuffer(Buffer buffer, RelicModel model, BufferManager? buffers = null)
    {
        buffer.Add(model.Title.GetFormattedText());

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            buffer.Add(StripBbcode(desc));

        if (model.ShowCounter && model.DisplayAmount != 0)
            buffer.Add($"Counter: {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            buffer.Add("Disabled");

        // Hover tips: skip first (it's the relic itself), rest are keywords/references
        try
        {
            var cardTips = new List<CardHoverTip>();
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is CardHoverTip cardTip)
                {
                    cardTips.Add(cardTip);
                }
                else if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(tipDesc))
                        buffer.Add($"{title}: {StripBbcode(tipDesc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }

            // Populate card buffer with referenced cards
            if (cardTips.Count > 0 && buffers != null)
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
    }
}
