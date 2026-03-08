using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
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
        Repopulate(Populate);
    }

    private void Populate()
    {
        var model = _model;
        if (model == null) return;

        // Name
        Add(model.Title);

        // Type and rarity
        var typeRarity = model.Type.ToString();
        if (model.Rarity != CardRarity.Common)
            typeRarity += $", {model.Rarity}";
        Add(typeRarity);

        // Energy cost
        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                Add("X energy");
            else
                Add($"{model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
        }

        // Star cost
        if (model.CurrentStarCost > 0)
            Add($"{model.CurrentStarCost}");

        // Description
        try
        {
            var desc = model.GetDescriptionForPile(PileType.Hand);
            if (!string.IsNullOrEmpty(desc))
                Add(desc);
        }
        catch
        {
            // Hand pile may fail outside combat — try without pile context
            try
            {
                var desc = model.GetDescriptionForPile(PileType.None);
                if (!string.IsNullOrEmpty(desc))
                    Add(desc);
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
                    Add($"Enchantment: {enchTitle} - {enchDesc}");
                else if (!string.IsNullOrEmpty(enchTitle))
                    Add($"Enchantment: {enchTitle}");

                if (enchant.ShowAmount && enchant.DisplayAmount != 0)
                    Add($"Enchantment amount: {enchant.DisplayAmount}");

                if (enchant.Status == EnchantmentStatus.Disabled)
                    Add("Enchantment disabled");
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
                        Add($"{title}: {desc}");
                    else if (!string.IsNullOrEmpty(title))
                        Add(title);
                    else if (!string.IsNullOrEmpty(desc))
                        Add(desc);
                }
            }
        }
        catch
        {
            // Hover tips may fail outside combat context
        }
    }
}
