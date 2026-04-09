using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyRelicHolder : ProxyElement
{
    private RelicModel? _model;

    public ProxyRelicHolder(Control control) : base(control) { }

    private ProxyRelicHolder(RelicModel model) : base()
    {
        _model = model;
    }

    public static ProxyRelicHolder FromModel(RelicModel model) => new(model);

    private RelicModel? GetModel()
    {
        if (_model != null) return _model;

        if (Control is NRelicInventoryHolder invHolder)
            return invHolder.Relic?.Model;

        if (Control is NTreasureRoomRelicHolder treasureHolder)
            return treasureHolder.Relic?.Model;

        if (Control is NRelicBasicHolder basicHolder)
            return basicHolder.Relic?.Model;

        return null;
    }

    public override Message? GetLabel()
    {
        var model = GetModel();
        if (model == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(model.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "relic";

    public override Message? GetStatusString()
    {
        var model = GetModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        if (model.ShowCounter && model.DisplayAmount != 0)
            parts.Add($"Counter {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            parts.Add("Disabled");

        return parts.Count > 0 ? Message.Raw(string.Join(", ", parts)) : null;
    }

    public override Message? GetTooltip()
    {
        var model = GetModel();
        if (model == null) return null;

        var desc = model.DynamicDescription.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? Message.Raw(StripBbcode(desc)) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetModel();
        if (model == null) return base.HandleBuffers(buffers);

        var relicBuffer = buffers.GetBuffer("relic") as RelicBuffer;
        if (relicBuffer != null)
        {
            relicBuffer.Bind(model);
            relicBuffer.Update();
            buffers.EnableBuffer("relic", true);
        }

        // Populate card buffer if relic has card hover tips
        var cardTips = RelicBuffer.GetCardTips(model);
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
                    CardBuffer.Populate(cardBuffer, cardTip.Card);
                }
                buffers.EnableBuffer("card", true);
            }
        }

        return "relic";
    }
}
