using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.UI.Elements;
namespace SayTheSpire2.Buffers;

public class CardBuffer : Buffer
{
    private CardModel? _model;

    public CardBuffer() : base("card") { }

    public void Bind(CardModel model)
    {
        _model = model;
    }

    protected override void ClearBinding()
    {
        _model = null;
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(() => Populate(this, _model));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with card data.
    /// Used by CardBuffer.Update(), and by other proxies that need card info
    /// (e.g., relic hover tips that reference cards).
    /// </summary>
    public static void Populate(Buffer buffer, CardModel model)
    {
        // Name, type, and rarity
        var header = $"{model.Title}, {model.Type}";
        if (model.Rarity != CardRarity.None)
            header += $", {model.Rarity}";
        buffer.Add(header);

        // Costs (energy + stars on one line)
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
        else if (model.CurrentStarCost >= 0)
        {
            try { costs.Add($"{model.GetStarCostWithModifiers()} stars"); }
            catch { costs.Add($"{model.CurrentStarCost} stars"); }
        }
        if (costs.Count > 0)
            buffer.Add(string.Join(", ", costs));

        // Description
        try
        {
            var desc = model.GetDescriptionForPile(PileType.Hand);
            if (!string.IsNullOrEmpty(desc))
                buffer.Add(ProxyElement.StripBbcode(desc));
        }
        catch
        {
            try
            {
                var desc = model.GetDescriptionForPile(PileType.None);
                if (!string.IsNullOrEmpty(desc))
                    buffer.Add(ProxyElement.StripBbcode(desc));
            }
            catch { }
        }

        // Enchantment
        if (model.Enchantment != null)
        {
            try
            {
                var enchant = model.Enchantment;
                var enchTitle = enchant.Title.GetFormattedText();
                var enchDesc = enchant.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(enchTitle) && !string.IsNullOrEmpty(enchDesc))
                    buffer.Add($"Enchantment: {enchTitle} - {ProxyElement.StripBbcode(enchDesc)}");
                else if (!string.IsNullOrEmpty(enchTitle))
                    buffer.Add($"Enchantment: {enchTitle}");

                if (enchant.ShowAmount && enchant.DisplayAmount != 0)
                    buffer.Add($"Enchantment amount: {enchant.DisplayAmount}");

                if (enchant.Status == EnchantmentStatus.Disabled)
                    buffer.Add("Enchantment disabled");
            }
            catch { }
        }

        // Hover tips (keywords, powers, etc.)
        try
        {
            foreach (var tip in model.HoverTips)
            {
                if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var desc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                        buffer.Add($"{title}: {ProxyElement.StripBbcode(desc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                    else if (!string.IsNullOrEmpty(desc))
                        buffer.Add(ProxyElement.StripBbcode(desc));
                }
            }
        }
        catch { }
    }
}
