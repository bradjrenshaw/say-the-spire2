using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
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
    public ProxyCard(Control control) : base(control) { }

    private NCardHolder? FindCardHolder()
    {
        if (Control is NCardHolder direct)
            return direct;
        Node? current = Control.GetParent();
        while (current != null)
        {
            if (current is NCardHolder holder)
                return holder;
            current = current.GetParent();
        }
        return null;
    }

    private CardModel? GetCardModel() => FindCardHolder()?.CardModel;

    public override string? GetLabel()
    {
        var model = GetCardModel();
        if (model == null) return CleanNodeName(Control.Name);
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

    public override string? GetTypeKey()
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
        else if (model.CurrentStarCost > 0)
            parts.Add(verbose ? $"{model.CurrentStarCost} stars" : $"{model.CurrentStarCost}");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? GetStatusString()
    {
        var model = GetCardModel();
        if (model == null) return null;

        if (model.Enchantment != null)
        {
            try { return $"Enchanted: {model.Enchantment.Title.GetFormattedText()}"; }
            catch { }
        }

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

    /// <summary>
    /// Populates a card buffer from any CardModel. Used by other proxies
    /// when their hover tips reference cards (e.g., relics that grant cards).
    /// </summary>
    public static void PopulateCardBuffer(Buffer buffer, CardModel model)
    {
        buffer.Add(model.Title);
        var typeRarity = model.Type.ToString();
        if (model.Rarity != CardRarity.Common)
            typeRarity += $", {model.Rarity}";
        buffer.Add(typeRarity);

        var costs = new System.Collections.Generic.List<string>();
        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                costs.Add("X energy");
            else
            {
                try { costs.Add($"{model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy"); }
                catch { costs.Add($"{model.EnergyCost.Canonical} energy"); }
            }
        }
        if (model.HasStarCostX)
            costs.Add("X stars");
        else if (model.CurrentStarCost > 0)
            costs.Add($"{model.CurrentStarCost} stars");
        if (costs.Count > 0)
            buffer.Add(string.Join(", ", costs));

        try
        {
            var desc = model.GetDescriptionForPile(PileType.None);
            if (!string.IsNullOrEmpty(desc))
                buffer.Add(StripBbcode(desc));
        }
        catch { }

        if (model.Enchantment != null)
        {
            try
            {
                var enchTitle = model.Enchantment.Title.GetFormattedText();
                var enchDesc = model.Enchantment.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(enchTitle) && !string.IsNullOrEmpty(enchDesc))
                    buffer.Add($"Enchantment: {enchTitle} - {StripBbcode(enchDesc)}");
                else if (!string.IsNullOrEmpty(enchTitle))
                    buffer.Add($"Enchantment: {enchTitle}");
            }
            catch { }
        }

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
