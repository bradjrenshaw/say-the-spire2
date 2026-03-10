using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyPotionHolder : ProxyElement
{
    private PotionModel? _model;

    public ProxyPotionHolder(Control control) : base(control) { }

    private ProxyPotionHolder(PotionModel model) : base()
    {
        _model = model;
    }

    public static ProxyPotionHolder FromModel(PotionModel model) => new(model);

    private NPotionHolder? Holder => Control as NPotionHolder;

    private PotionModel? GetModel()
    {
        if (_model != null) return _model;
        var holder = Holder;
        if (holder == null || !holder.HasPotion) return null;
        return holder.Potion!.Model;
    }

    private bool IsEmpty()
    {
        if (_model != null) return false;
        var holder = Holder;
        return holder == null || !holder.HasPotion;
    }

    public override string? GetLabel()
    {
        if (IsEmpty())
            return "Empty potion slot";

        var model = GetModel();
        return model?.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "potion";

    public override string? GetTooltip()
    {
        var model = GetModel();
        if (model == null) return null;

        var desc = model.DynamicDescription.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? StripBbcode(desc) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        if (IsEmpty())
        {
            var uiBuffer = buffers.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                uiBuffer.Add("Empty potion slot");
                var slotDesc = new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "POTION_SLOT.description").GetFormattedText();
                if (!string.IsNullOrEmpty(slotDesc))
                    uiBuffer.Add(StripBbcode(slotDesc));
                buffers.EnableBuffer("ui", true);
            }
            return "ui";
        }

        var model = GetModel();
        if (model == null) return base.HandleBuffers(buffers);

        var potionBuffer = buffers.GetBuffer("potion") as PotionBuffer;
        if (potionBuffer != null)
        {
            potionBuffer.Bind(model);
            potionBuffer.Update();
            buffers.EnableBuffer("potion", true);
        }

        return "potion";
    }
}
