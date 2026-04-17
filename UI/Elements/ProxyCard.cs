using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(EnergyCostAnnouncement),
    typeof(StarCostAnnouncement),
    typeof(SubtypeAnnouncement),
    typeof(TypeAnnouncement),
    // Shop-context insertion points — ProxyCard never yields these, but
    // ProxyMerchantSlot does, and card's order positions them here.
    typeof(PriceAnnouncement),
    typeof(SoldOutAnnouncement),
    // Grid-selection insertion points — injected via CollectAnnouncements in
    // CardGridSelectionGameScreen when a card is part of the active selection.
    typeof(SelectedMarkerAnnouncement),
    typeof(SelectionCountAnnouncement),
    typeof(TooltipAnnouncement)
)]
[ModSettings("ui.card", "UI/Card")]
public class ProxyCard : ProxyElement
{
    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose_costs", "Verbose Costs", true));
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = GetView();
        if (view == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        // Label (with enchantment/affliction in parens when present)
        var modifiers = new List<string>();
        if (view.EnchantmentTitle is { Length: > 0 } ench) modifiers.Add(ench);
        if (view.AfflictionTitle is { Length: > 0 } aff) modifiers.Add(aff);
        var labelText = modifiers.Count > 0
            ? $"{view.Title} ({string.Join(", ", modifiers)})"
            : view.Title;
        yield return new LabelAnnouncement(labelText);

        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");

        // Energy cost (skipped entirely if the card has no energy cost concept)
        var energyCost = view.EnergyCost;
        if (energyCost != null)
        {
            if (energyCost.CostsX)
                yield return new EnergyCostAnnouncement(0, isX: true, verbose);
            else
                yield return new EnergyCostAnnouncement(energyCost.GetWithModifiers(CostModifiers.All), isX: false, verbose);
        }

        // Star cost
        if (view.HasStarCostX)
            yield return new StarCostAnnouncement(0, isX: true, verbose);
        else if (view.CurrentStarCost >= 0)
            yield return new StarCostAnnouncement(view.StarCostWithModifiers, isX: false, verbose);

        // Subtype + type
        yield return new SubtypeAnnouncement(view.TypeKey);
        yield return new TypeAnnouncement("card");

        // Tooltip
        if (!string.IsNullOrEmpty(view.Description))
            yield return new TooltipAnnouncement(view.Description);
    }

    private readonly CardModel? _model;

    public ProxyCard(Control control) : base(control) { }

    private ProxyCard(CardModel model) : base()
    {
        _model = model;
    }

    public static ProxyCard FromModel(CardModel model) => new(model);

    private CardView? GetView() =>
        _model != null ? CardView.FromModel(_model) : CardView.FromControl(Control);

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        var modifiers = new List<string>();
        if (view.EnchantmentTitle is { Length: > 0 } ench) modifiers.Add(ench);
        if (view.AfflictionTitle is { Length: > 0 } aff) modifiers.Add(aff);
        if (modifiers.Count > 0)
            return Message.Raw($"{view.Title} ({string.Join(", ", modifiers)})");
        return Message.Raw(view.Title);
    }

    public override string? GetTypeKey() => "card";

    public override string? GetSubtypeKey() => GetView()?.TypeKey;

    public override Message? GetExtrasString()
    {
        var view = GetView();
        if (view == null) return null;

        var parts = new List<string>();
        bool verbose = ModSettings.GetValue<bool>("ui.card.verbose_costs");

        var energyCost = view.EnergyCost;
        if (energyCost != null)
        {
            if (energyCost.CostsX)
                parts.Add(verbose ? LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_ENERGY", "X energy") : "X");
            else
            {
                var cost = energyCost.GetWithModifiers(CostModifiers.All);
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost }).Resolve() : $"{cost}");
            }
        }

        if (view.HasStarCostX)
            parts.Add(verbose ? LocalizationManager.GetOrDefault("ui", "RESOURCE.CARD_X_STARS", "X stars") : "X");
        else if (view.CurrentStarCost >= 0)
        {
            var starCost = view.StarCostWithModifiers;
            parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = starCost }).Resolve() : $"{starCost}");
        }

        return parts.Count > 0 ? Message.Raw(string.Join(", ", parts)) : null;
    }

    public override Message? GetTooltip()
    {
        var desc = GetView()?.Description;
        return string.IsNullOrEmpty(desc) ? null : Message.Raw(desc);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null) return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card") as CardBuffer;
        if (cardBuffer != null)
        {
            var statsText = view.LibraryStatsText;
            cardBuffer.Bind(view.DisplayedModel, statsText == null ? null : new[] { statsText });
            cardBuffer.Update();
            buffers.EnableBuffer("card", true);
        }

        var upgradeBuffer = buffers.GetBuffer("upgrade") as UpgradeBuffer;
        if (upgradeBuffer != null)
        {
            if (view.IsShowingUpgradedCard)
                upgradeBuffer.BindUnavailable();
            else
                upgradeBuffer.Bind(view.BaseModel);
            upgradeBuffer.Update();
            buffers.EnableBuffer("upgrade", true);
        }

        return "card";
    }
}
