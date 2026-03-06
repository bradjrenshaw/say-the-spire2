using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using Sts2AccessibilityMod.Buffers;

namespace Sts2AccessibilityMod.UI;

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
        return model.Title;
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
                parts.Add($"{model.EnergyCost.Canonical} energy");
        }

        // Star cost
        if (model.CurrentStarCost > 0)
            parts.Add($"{model.CurrentStarCost} stars");

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
                    cardBuffer.Add($"Cost: {model.EnergyCost.Canonical} energy");
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
                // Description may fail if not in combat context
                var desc = model.Description.GetFormattedText();
                if (!string.IsNullOrEmpty(desc))
                    cardBuffer.Add(StripBbcode(desc));
            }

            // Rarity
            if (model.Rarity != CardRarity.Common)
                cardBuffer.Add(model.Rarity.ToString());

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

        // Also populate the player buffer during combat
        PlayerBufferHelper.Populate(buffers);

        return "card";
    }
}
