using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(EnergyCostAnnouncement),
    typeof(StarCostAnnouncement),
    typeof(SubtypeAnnouncement),
    typeof(TypeAnnouncement),
    typeof(TooltipAnnouncement)
)]
[ModSettings("ui.card", "UI/Card")]
public class ProxyDeckHistoryEntry : ProxyElement
{
    private static readonly FieldInfo? AmountField =
        AccessTools.Field(typeof(NDeckHistoryEntry), "_amount");

    public ProxyDeckHistoryEntry(Control control) : base(control) { }

    private NDeckHistoryEntry? Entry => Control as NDeckHistoryEntry;
    private CardModel? Card => Entry?.Card;
    private int Amount => AmountField?.GetValue(Entry) as int? ?? 1;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var model = Card;
        if (model == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        // Label uses GetLabel since it folds in quantity + enchantment/affliction
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");

        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                yield return new EnergyCostAnnouncement(0, isX: true, verbose);
            else
                yield return new EnergyCostAnnouncement(model.EnergyCost.GetWithModifiers(CostModifiers.All), isX: false, verbose);
        }

        if (model.HasStarCostX)
            yield return new StarCostAnnouncement(0, isX: true, verbose);
        else if (model.CurrentStarCost >= 0)
        {
            int starCost;
            try { starCost = model.GetStarCostWithModifiers(); }
            catch (System.Exception e) { Log.Info($"[AccessibilityMod] GetStarCostWithModifiers failed: {e.Message}"); starCost = model.CurrentStarCost; }
            yield return new StarCostAnnouncement(starCost, isX: false, verbose);
        }

        yield return new SubtypeAnnouncement(model.Type.ToString().ToLowerInvariant());
        yield return new TypeAnnouncement("card");

        var desc = model.GetDescriptionForPile(PileType.None);
        if (!string.IsNullOrEmpty(desc))
            yield return new TooltipAnnouncement(StripBbcode(desc));
    }

    public override Message? GetLabel()
    {
        var model = Card;
        if (model == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

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

        return Amount > 1 ? Message.Localized("ui", "CARD.QUANTITY", new { amount = Amount, title }) : Message.Raw(title);
    }

    public override string? GetTypeKey() => "card";

    public override string? GetSubtypeKey()
    {
        var model = Card;
        return model?.Type.ToString().ToLower();
    }

    public override Message? GetExtrasString()
    {
        var model = Card;
        if (model == null)
            return null;

        var parts = new List<string>();
        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");

        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                parts.Add(verbose ? LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_ENERGY", "X energy") : "X");
            else
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = model.EnergyCost.GetWithModifiers(CostModifiers.All) }).Resolve() : $"{model.EnergyCost.GetWithModifiers(CostModifiers.All)}");
        }

        if (model.HasStarCostX)
            parts.Add(verbose ? LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_STARS", "X stars") : "X");
        else if (model.CurrentStarCost >= 0)
        {
            int starCost;
            try { starCost = model.GetStarCostWithModifiers(); }
            catch (System.Exception e) { Log.Info($"[AccessibilityMod] GetStarCostWithModifiers failed: {e.Message}"); starCost = model.CurrentStarCost; }
            parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = starCost }).Resolve() : $"{starCost}");
        }

        return parts.Count > 0 ? Message.Raw(string.Join(", ", parts)) : null;
    }

    public override Message? GetTooltip()
    {
        var model = Card;
        if (model == null)
            return null;

        var desc = model.GetDescriptionForPile(PileType.None);
        return string.IsNullOrEmpty(desc) ? null : Message.Raw(StripBbcode(desc));
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
