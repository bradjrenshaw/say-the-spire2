using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyMerchantSlot : ProxyElement
{
    public ProxyMerchantSlot(Control control) : base(control) { }

    private NMerchantSlot? Slot => Control as NMerchantSlot;

    private MerchantEntry? GetEntry()
    {
        try { return Slot?.Entry; }
        catch { return null; }
    }

    public override string? GetLabel()
    {
        var entry = GetEntry();
        if (entry == null) return CleanNodeName(Control.Name);

        switch (entry)
        {
            case MerchantCardEntry cardEntry:
                var card = cardEntry.CreationResult?.Card;
                return card?.Title ?? "Empty";

            case MerchantRelicEntry relicEntry:
                return relicEntry.Model?.Title.GetFormattedText() ?? "Empty";

            case MerchantPotionEntry potionEntry:
                return potionEntry.Model?.Title.GetFormattedText() ?? "Empty";

            case MerchantCardRemovalEntry:
                return "Card Removal";

            default:
                return CleanNodeName(Control.Name);
        }
    }

    public override string? GetTypeKey() => "shop item";

    public override string? GetStatusString()
    {
        var entry = GetEntry();
        if (entry == null) return null;

        if (!entry.IsStocked) return "Sold out";

        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"{entry.Cost} gold");

        if (!entry.EnoughGold)
            parts.Add("Not enough gold");

        if (entry is MerchantCardEntry cardEntry)
        {
            var card = cardEntry.CreationResult?.Card;
            if (card != null)
            {
                parts.Add(card.Type.ToString());
                if (card.EnergyCost != null)
                {
                    if (card.EnergyCost.CostsX)
                        parts.Add("X energy");
                    else
                        parts.Add($"{card.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
                }
            }
            if (cardEntry.IsOnSale)
                parts.Add("On sale");
        }

        return string.Join(", ", parts);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entry = GetEntry();
        if (entry == null) return base.HandleBuffers(buffers);

        switch (entry)
        {
            case MerchantCardEntry cardEntry:
                return HandleCardBuffers(buffers, cardEntry);

            case MerchantRelicEntry relicEntry:
                return HandleRelicBuffers(buffers, relicEntry);

            case MerchantPotionEntry potionEntry:
                return HandlePotionBuffers(buffers, potionEntry);

            case MerchantCardRemovalEntry removalEntry:
                return HandleRemovalBuffers(buffers, removalEntry);

            default:
                return base.HandleBuffers(buffers);
        }
    }

    private string? HandleCardBuffers(BufferManager buffers, MerchantCardEntry cardEntry)
    {
        var card = cardEntry.CreationResult?.Card;
        if (card == null) return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card");
        if (cardBuffer != null)
        {
            cardBuffer.Clear();

            cardBuffer.Add(card.Title);
            cardBuffer.Add(card.Type.ToString());

            if (card.EnergyCost != null)
            {
                if (card.EnergyCost.CostsX)
                    cardBuffer.Add("Cost: X energy");
                else
                    cardBuffer.Add($"Cost: {card.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
            }

            if (card.CurrentStarCost > 0)
                cardBuffer.Add($"Star cost: {card.CurrentStarCost}");

            try
            {
                var desc = card.GetDescriptionForPile(PileType.None);
                if (!string.IsNullOrEmpty(desc))
                    cardBuffer.Add(StripBbcode(desc));
            }
            catch { }

            if (card.Rarity != CardRarity.Common)
                cardBuffer.Add(card.Rarity.ToString());

            if (card.Enchantment != null)
            {
                try
                {
                    var enchTitle = card.Enchantment.Title.GetFormattedText();
                    var enchDesc = card.Enchantment.DynamicDescription.GetFormattedText();
                    if (!string.IsNullOrEmpty(enchTitle) && !string.IsNullOrEmpty(enchDesc))
                        cardBuffer.Add($"Enchantment: {enchTitle} - {StripBbcode(enchDesc)}");
                    else if (!string.IsNullOrEmpty(enchTitle))
                        cardBuffer.Add($"Enchantment: {enchTitle}");
                }
                catch { }
            }

            cardBuffer.Add($"Price: {cardEntry.Cost} gold");
            if (cardEntry.IsOnSale)
                cardBuffer.Add("On sale!");

            try
            {
                foreach (var tip in card.HoverTips)
                {
                    if (tip is HoverTip ht)
                    {
                        var title = ht.Title;
                        var desc = ht.Description;
                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                            cardBuffer.Add($"{title}: {StripBbcode(desc)}");
                        else if (!string.IsNullOrEmpty(title))
                            cardBuffer.Add(title);
                    }
                }
            }
            catch { }

            buffers.EnableBuffer("card", true);
        }

        // Upgrade preview
        var upgradeBuffer = buffers.GetBuffer("upgrade");
        if (upgradeBuffer != null)
        {
            upgradeBuffer.Clear();

            if (!card.IsUpgradable)
            {
                upgradeBuffer.Add("No upgrade available");
            }
            else if (card.CardScope != null)
            {
                try
                {
                    var clone = card.CardScope.CloneCard(card);
                    clone.UpgradeInternal();

                    upgradeBuffer.Add(clone.Title);
                    upgradeBuffer.Add(clone.Type.ToString());

                    if (clone.EnergyCost != null)
                    {
                        if (clone.EnergyCost.CostsX)
                            upgradeBuffer.Add("Cost: X energy");
                        else
                            upgradeBuffer.Add($"Cost: {clone.EnergyCost.Canonical} energy");
                    }

                    try
                    {
                        var desc = clone.GetDescriptionForUpgradePreview();
                        if (!string.IsNullOrEmpty(desc))
                            upgradeBuffer.Add(StripBbcode(desc));
                    }
                    catch { }
                }
                catch
                {
                    upgradeBuffer.Add("Upgrade preview unavailable");
                }
            }

            buffers.EnableBuffer("upgrade", true);
        }

        return "card";
    }

    private string? HandleRelicBuffers(BufferManager buffers, MerchantRelicEntry relicEntry)
    {
        var model = relicEntry.Model;
        if (model == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            ProxyRelicHolder.PopulateRelicBuffer(uiBuffer, model, buffers);
            uiBuffer.Add($"Price: {relicEntry.Cost} gold");
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }

    private string? HandlePotionBuffers(BufferManager buffers, MerchantPotionEntry potionEntry)
    {
        var model = potionEntry.Model;
        if (model == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            ProxyPotionHolder.PopulatePotionBuffer(uiBuffer, model);
            uiBuffer.Add($"Price: {potionEntry.Cost} gold");
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }

    private string? HandleRemovalBuffers(BufferManager buffers, MerchantCardRemovalEntry removalEntry)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            uiBuffer.Add("Card Removal Service");
            uiBuffer.Add($"Price: {removalEntry.Cost} gold");
            if (!removalEntry.IsStocked)
                uiBuffer.Add("Already used");
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
