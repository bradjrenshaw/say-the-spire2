using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Elements;

[ModSettings("ui.card", "UI/Card")]
public class ProxyDeckHistoryEntry : ProxyElement
{
    private static readonly FieldInfo? AmountField =
        AccessTools.Field(typeof(NDeckHistoryEntry), "_amount");

    public ProxyDeckHistoryEntry(Control control) : base(control) { }

    private NDeckHistoryEntry? Entry => Control as NDeckHistoryEntry;
    private CardModel? Card => Entry?.Card;
    private int Amount => AmountField?.GetValue(Entry) as int? ?? 1;

    public override string? GetLabel()
    {
        var model = Card;
        if (model == null)
            return Control != null ? CleanNodeName(Control.Name) : null;

        var title = model.Title;
        var modifiers = new List<string>();
        var enchantTitle = model.Enchantment?.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(enchantTitle))
            modifiers.Add(enchantTitle);
        var afflictionTitle = model.Affliction?.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(afflictionTitle))
            modifiers.Add(afflictionTitle);
        if (modifiers.Count > 0)
            title = $"{title} ({string.Join(", ", modifiers)})";

        return Amount > 1 ? $"{Amount}x {title}" : title;
    }

    public override string? GetTypeKey() => "card";

    public override string? GetSubtypeKey()
    {
        var model = Card;
        return model?.Type.ToString().ToLower();
    }

    public override string? GetExtrasString()
    {
        var model = Card;
        if (model == null)
            return null;

        var parts = new List<string>();
        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");

        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                parts.Add(verbose ? "X energy" : "X");
            else
                parts.Add(verbose ? $"{model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy" : $"{model.EnergyCost.GetWithModifiers(CostModifiers.All)}");
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
        var model = Card;
        if (model == null)
            return null;

        var desc = model.GetDescriptionForPile(PileType.None);
        return string.IsNullOrEmpty(desc) ? null : StripBbcode(desc);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = Card;
        if (model == null)
            return base.HandleBuffers(buffers);

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
