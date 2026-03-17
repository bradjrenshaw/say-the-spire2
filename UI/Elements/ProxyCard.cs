using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SayTheSpire2.Buffers;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Elements;

[ModSettings("ui.card", "UI/Card")]
public class ProxyCard : ProxyElement
{
    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose_costs", "Verbose Costs", true));
    }

    private CardModel? _model;

    public ProxyCard(Control control) : base(control) { }

    private ProxyCard(CardModel model) : base()
    {
        _model = model;
    }

    public static ProxyCard FromModel(CardModel model) => new(model);

    private NCardHolder? FindCardHolder()
    {
        if (Control is NCardHolder direct)
            return direct;
        Node? current = Control?.GetParent();
        while (current != null)
        {
            if (current is NCardHolder holder)
                return holder;
            current = current.GetParent();
        }
        return null;
    }

    private CardModel? GetCardModel() => _model ?? FindCardHolder()?.CardModel;

    public override string? GetLabel()
    {
        var model = GetCardModel();
        if (model == null) return Control != null ? CleanNodeName(Control.Name) : null;
        var title = model.Title;
        var modifiers = new System.Collections.Generic.List<string>();
        var enchantTitle = model.Enchantment?.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(enchantTitle)) modifiers.Add(enchantTitle);
        var afflictionTitle = model.Affliction?.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(afflictionTitle)) modifiers.Add(afflictionTitle);
        if (modifiers.Count > 0)
            return $"{title} ({string.Join(", ", modifiers)})";
        return title;
    }

    public override string? GetTypeKey() => "card";

    public override string? GetSubtypeKey()
    {
        var model = GetCardModel();
        if (model == null) return null;
        return model.Type.ToString().ToLower();
    }

    public override string? GetExtrasString()
    {
        var model = GetCardModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");
        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                parts.Add(verbose ? "X energy" : "X");
            else
            {
                var cost = model.EnergyCost.GetWithModifiers(CostModifiers.All);
                parts.Add(verbose ? $"{cost} energy" : $"{cost}");
            }
        }

        if (model.HasStarCostX)
            parts.Add(verbose ? "X stars" : "X");
        else if (model.CurrentStarCost >= 0)
        {
            int starCost;
            try { starCost = model.GetStarCostWithModifiers(); }
            catch { starCost = model.CurrentStarCost; }
            parts.Add(verbose ? $"{starCost} stars" : $"{starCost}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? GetTooltip()
    {
        var model = GetCardModel();
        if (model == null) return null;

        try
        {
            var desc = model.GetDescriptionForPile(PileType.None);
            if (!string.IsNullOrEmpty(desc))
                return StripBbcode(desc);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card tooltip description failed: {e.Message}"); }

        return null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetCardModel();
        if (model == null) return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card") as CardBuffer;
        if (cardBuffer != null)
        {
            cardBuffer.Bind(model);
            cardBuffer.Update();
            buffers.EnableBuffer("card", true);
        }

        var upgradeBuffer = buffers.GetBuffer("upgrade") as UpgradeBuffer;
        if (upgradeBuffer != null)
        {
            upgradeBuffer.Bind(model);
            upgradeBuffer.Update();
            buffers.EnableBuffer("upgrade", true);
        }

        return "card";
    }
}
