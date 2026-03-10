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

        if (model.ShowCounter && model.DisplayAmount != 0)
            parts.Add($"Counter {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            parts.Add("Disabled");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? GetTooltip()
    {
        var model = GetModel();
        if (model == null) return null;

        var desc = model.DynamicDescription.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? StripBbcode(desc) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetModel();
        if (model == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            RelicBuffer.PopulateBuffer(uiBuffer, model, buffers);
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
