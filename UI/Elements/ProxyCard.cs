using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyCard : ProxyElement
{
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

    public override string? GetStatusString()
    {
        var model = GetCardModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        // Energy cost
        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                parts.Add("X energy");
            else
                parts.Add($"{model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
        }

        // Star cost
        if (model.CurrentStarCost > 0)
            parts.Add($"{model.CurrentStarCost} stars");

        // Enchantment
        if (model.Enchantment != null)
        {
            try { parts.Add($"Enchanted: {model.Enchantment.Title.GetFormattedText()}"); }
            catch { }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetCardModel();
        if (model == null) return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card");
        if (cardBuffer != null)
        {
            cardBuffer.Clear();

            // Name
            cardBuffer.Add(model.Title);

            // Type
            cardBuffer.Add(model.Type.ToString());

            // Energy cost
            if (model.EnergyCost != null)
            {
                if (model.EnergyCost.CostsX)
                    cardBuffer.Add("Cost: X energy");
                else
                    cardBuffer.Add($"Cost: {model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
            }

            // Star cost
            if (model.CurrentStarCost > 0)
                cardBuffer.Add($"Star cost: {model.CurrentStarCost}");

            // Description
            try
            {
                var desc = model.GetDescriptionForPile(PileType.Hand);
                if (!string.IsNullOrEmpty(desc))
                    cardBuffer.Add(StripBbcode(desc));
            }
            catch
            {
                // Hand pile may fail outside combat — try without pile context
                try
                {
                    var desc = model.GetDescriptionForPile(PileType.None);
                    if (!string.IsNullOrEmpty(desc))
                        cardBuffer.Add(StripBbcode(desc));
                }
                catch { }
            }

            // Rarity
            if (model.Rarity != CardRarity.Common)
                cardBuffer.Add(model.Rarity.ToString());

            // Enchantment
            if (model.Enchantment != null)
            {
                try
                {
                    var enchant = model.Enchantment;
                    var enchTitle = enchant.Title.GetFormattedText();
                    var enchDesc = enchant.DynamicDescription.GetFormattedText();
                    if (!string.IsNullOrEmpty(enchTitle) && !string.IsNullOrEmpty(enchDesc))
                        cardBuffer.Add($"Enchantment: {enchTitle} - {StripBbcode(enchDesc)}");
                    else if (!string.IsNullOrEmpty(enchTitle))
                        cardBuffer.Add($"Enchantment: {enchTitle}");

                    if (enchant.ShowAmount && enchant.DisplayAmount != 0)
                        cardBuffer.Add($"Enchantment amount: {enchant.DisplayAmount}");

                    if (enchant.Status == EnchantmentStatus.Disabled)
                        cardBuffer.Add("Enchantment disabled");
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
                            cardBuffer.Add($"{title}: {StripBbcode(desc)}");
                        else if (!string.IsNullOrEmpty(title))
                            cardBuffer.Add(title);
                        else if (!string.IsNullOrEmpty(desc))
                            cardBuffer.Add(StripBbcode(desc));
                    }
                }
            }
            catch
            {
                // Hover tips may fail outside combat context
            }

            buffers.EnableBuffer("card", true);
        }

        // Upgrade preview buffer
        var upgradeBuffer = buffers.GetBuffer("upgrade");
        if (upgradeBuffer != null)
        {
            upgradeBuffer.Clear();

            if (!model.IsUpgradable)
            {
                upgradeBuffer.Add("No upgrade available");
            }
            else if (model.CardScope != null)
            {
                try
                {
                    var clone = model.CardScope.CloneCard(model);
                    clone.UpgradeInternal();

                    upgradeBuffer.Add(clone.Title);
                    upgradeBuffer.Add(clone.Type.ToString());

                    if (clone.EnergyCost != null)
                    {
                        if (clone.EnergyCost.CostsX)
                            upgradeBuffer.Add("Cost: X energy");
                        else
                            upgradeBuffer.Add($"Cost: {clone.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
                    }

                    if (clone.CurrentStarCost > 0)
                        upgradeBuffer.Add($"Star cost: {clone.CurrentStarCost}");

                    try
                    {
                        var desc = clone.GetDescriptionForUpgradePreview();
                        if (!string.IsNullOrEmpty(desc))
                            upgradeBuffer.Add(StripBbcode(desc));
                    }
                    catch { }
                }
                catch (System.Exception e)
                {
                    Log.Error($"[AccessibilityMod] Card upgrade preview failed: {e.Message}");
                    upgradeBuffer.Add("Upgrade preview unavailable");
                }
            }

            buffers.EnableBuffer("upgrade", true);
        }

        // Also populate the player buffer during combat
        PlayerBufferHelper.Populate(buffers);

        return "card";
    }

    /// <summary>
    /// Populates a card buffer from any CardModel. Used by other proxies
    /// when their hover tips reference cards (e.g., relics that grant cards).
    /// </summary>
    public static void PopulateCardBuffer(Buffer buffer, CardModel model)
    {
        buffer.Add(model.Title);
        buffer.Add(model.Type.ToString());

        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                buffer.Add("Cost: X energy");
            else
            {
                try { buffer.Add($"Cost: {model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy"); }
                catch { buffer.Add($"Cost: {model.EnergyCost.Canonical} energy"); }
            }
        }

        if (model.CurrentStarCost > 0)
            buffer.Add($"Star cost: {model.CurrentStarCost}");

        try
        {
            var desc = model.GetDescriptionForPile(PileType.None);
            if (!string.IsNullOrEmpty(desc))
                buffer.Add(StripBbcode(desc));
        }
        catch { }

        if (model.Rarity != CardRarity.Common)
            buffer.Add(model.Rarity.ToString());

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
