using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;
namespace SayTheSpire2.Buffers;

public class CardBuffer : Buffer
{
    private CardModel? _model;
    private IReadOnlyList<string> _extraLines = Array.Empty<string>();

    public CardBuffer() : base("card") { }

    public void Bind(CardModel model)
    {
        _model = model;
        _extraLines = Array.Empty<string>();
    }

    public void Bind(CardModel model, IEnumerable<string>? extraLines)
    {
        _model = model;
        _extraLines = extraLines?
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray()
            ?? Array.Empty<string>();
    }

    protected override void ClearBinding()
    {
        _model = null;
        _extraLines = Array.Empty<string>();
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(() => Populate(this, _model, _extraLines));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with card data.
    /// Used by CardBuffer.Update(), and by other proxies that need card info
    /// (e.g., relic hover tips that reference cards).
    /// </summary>
    public static void Populate(Buffer buffer, CardModel model, IEnumerable<string>? extraLines = null)
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
                costs.Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_ENERGY", "X energy"));
            else
            {
                try { costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = model.EnergyCost.GetWithModifiers(CostModifiers.All) }).Resolve()); }
                catch (System.Exception e) { Log.Info($"[AccessibilityMod] Energy cost modifier failed: {e.Message}"); costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = model.EnergyCost.Canonical }).Resolve()); }
            }
        }
        if (model.HasStarCostX)
            costs.Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_STARS", "X stars"));
        else if (model.CurrentStarCost >= 0)
        {
            try { costs.Add(Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = model.GetStarCostWithModifiers() }).Resolve()); }
            catch (System.Exception e) { Log.Info($"[AccessibilityMod] Star cost modifier failed: {e.Message}"); costs.Add(Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = model.CurrentStarCost }).Resolve()); }
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
            catch (Exception e) { Log.Error($"[AccessibilityMod] Card description fallback failed: {e.Message}"); }
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
                    buffer.Add(Message.Localized("ui", "CARD.ENCHANTMENT", new { title = enchTitle, description = ProxyElement.StripBbcode(enchDesc) }).Resolve());
                else if (!string.IsNullOrEmpty(enchTitle))
                    buffer.Add(Message.Localized("ui", "CARD.ENCHANTMENT_NO_DESC", new { title = enchTitle }).Resolve());

                if (enchant.ShowAmount && enchant.DisplayAmount != 0)
                    buffer.Add(Message.Localized("ui", "CARD.ENCHANTMENT_AMOUNT", new { amount = enchant.DisplayAmount }).Resolve());

                if (enchant.Status == EnchantmentStatus.Disabled)
                    buffer.Add(LocalizationManager.GetOrDefault("ui", "CARD.ENCHANTMENT_DISABLED", "Enchantment disabled"));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Card enchantment access failed: {e.Message}"); }
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
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card hover tips access failed: {e.Message}"); }

        if (extraLines == null)
            return;

        foreach (var line in extraLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                buffer.Add(line.Trim());
        }
    }
}
