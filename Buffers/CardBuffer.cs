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
using SayTheSpire2.Views;
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
    /// (e.g., relic hover tips that reference cards). Reads everything via
    /// CardView so the verbose buffer and the focus path agree on which
    /// fields exist and where they come from.
    /// </summary>
    public static void Populate(Buffer buffer, CardModel model, IEnumerable<string>? extraLines = null)
    {
        var view = CardView.FromModel(model);

        // Name, type, and rarity
        var typeText = LocalizationManager.GetOrDefault("ui", $"TYPES.{view.Type.ToString().ToUpperInvariant()}", view.Type.ToString());
        var header = $"{view.Title}, {typeText}";
        if (view.Rarity != CardRarity.None)
        {
            var rarityText = LocalizationManager.GetOrDefault("ui", $"RARITIES.{view.Rarity.ToString().ToUpperInvariant()}", view.Rarity.ToString());
            header += $", {rarityText}";
        }
        buffer.Add(header);

        // Costs (energy + stars on one line)
        var costs = new System.Collections.Generic.List<Message>();
        if (view.EnergyCost != null)
        {
            if (view.EnergyCost.CostsX)
                costs.Add(Message.Localized("ui", "RESOURCE.CARD_X_ENERGY"));
            else
            {
                try { costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = view.EnergyCost.GetWithModifiers(CostModifiers.All) })); }
                catch (System.Exception e) { Log.Info($"[AccessibilityMod] Energy cost modifier failed: {e.Message}"); costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = view.EnergyCost.Canonical })); }
            }
        }
        if (view.HasStarCostX)
            costs.Add(Message.Localized("ui", "RESOURCE.CARD_X_STARS"));
        else if (view.CurrentStarCost >= 0)
            costs.Add(Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = view.StarCostWithModifiers }));
        if (costs.Count > 0)
            buffer.Add(Message.Join(", ", costs.ToArray()).Resolve());

        // Description: prefer the in-hand variant (richer wording), fall back
        // to PileType.None. Pull the model directly because the pile-type
        // dispatch isn't a CardView concern.
        try
        {
            var desc = view.DisplayedModel.GetDescriptionForPile(PileType.Hand);
            if (!string.IsNullOrEmpty(desc))
                buffer.Add(ProxyElement.StripBbcode(desc));
        }
        catch
        {
            try
            {
                var desc = view.DisplayedModel.GetDescriptionForPile(PileType.None);
                if (!string.IsNullOrEmpty(desc))
                    buffer.Add(ProxyElement.StripBbcode(desc));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Card description fallback failed: {e.Message}"); }
        }

        // Enchantment
        if (view.Enchantment is { } enchant)
        {
            try
            {
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

        // Affliction
        if (view.Affliction is { } affliction)
        {
            try
            {
                var afflictTitle = affliction.Title.GetFormattedText();
                var afflictDesc = affliction.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(afflictTitle) && !string.IsNullOrEmpty(afflictDesc))
                    buffer.Add(Message.Localized("ui", "CARD.AFFLICTION", new { title = afflictTitle, description = ProxyElement.StripBbcode(afflictDesc) }).Resolve());
                else if (!string.IsNullOrEmpty(afflictTitle))
                    buffer.Add(Message.Localized("ui", "CARD.AFFLICTION_NO_DESC", new { title = afflictTitle }).Resolve());

                if (affliction.IsStackable && affliction.Amount > 0)
                    buffer.Add(Message.Localized("ui", "CARD.AFFLICTION_AMOUNT", new { amount = affliction.Amount }).Resolve());
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Card affliction access failed: {e.Message}"); }
        }

        // Hover tips (keywords, powers, etc.)
        try
        {
            foreach (var tip in view.HoverTips)
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
