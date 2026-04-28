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
    typeof(ModifiersAnnouncement),
    typeof(EnergyCostAnnouncement),
    typeof(SubtypeAnnouncement),
    typeof(TypeAnnouncement),
    // Shop-context insertion point — ProxyCard never yields this, but
    // ProxyMerchantSlot does, and card's order positions it here.
    typeof(PriceAnnouncement),
    // Grid-selection insertion points — injected via CollectAnnouncements in
    // CardGridSelectionGameScreen when a card is part of the active selection.
    typeof(SelectedMarkerAnnouncement),
    typeof(SelectionCountAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyCard : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = GetView();
        if (view == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        yield return new LabelAnnouncement(view.Title);

        // Replay / enchantment / affliction together — the announcement decides
        // which parts to render based on per-element / global settings, and
        // emits an empty message if nothing applies.
        if (view.ReplayCount > 0
            || !string.IsNullOrEmpty(view.EnchantmentTitle)
            || !string.IsNullOrEmpty(view.AfflictionTitle))
            yield return new ModifiersAnnouncement(view.ReplayCount, view.EnchantmentTitle, view.AfflictionTitle);

        // Energy + star cost (skipped when the card has neither)
        int? energyCost = null;
        bool energyIsX = false;
        var energy = view.EnergyCost;
        if (energy != null)
        {
            if (energy.CostsX) { energyCost = 0; energyIsX = true; }
            else energyCost = energy.GetWithModifiers(CostModifiers.All);
        }

        int? starCost = null;
        bool starIsX = false;
        if (view.HasStarCostX) { starCost = 0; starIsX = true; }
        else if (view.CurrentStarCost >= 0) starCost = view.StarCostWithModifiers;

        if (energyCost.HasValue || starCost.HasValue)
            yield return new EnergyCostAnnouncement(energyCost, energyIsX, starCost, starIsX);

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
        return Message.Raw(view.Title);
    }

    public override string? GetTypeKey() => "card";

    public override string? GetSubtypeKey() => GetView()?.TypeKey;

    public override Message? GetExtrasString()
    {
        var view = GetView();
        if (view == null) return null;

        var parts = new List<Message>();
        bool verbose = ModSettings.GetValue<bool>("announcements.energy_cost.verbose");

        var energyCost = view.EnergyCost;
        if (energyCost != null)
        {
            if (energyCost.CostsX)
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_X_ENERGY") : Message.Raw("X"));
            else
            {
                var cost = energyCost.GetWithModifiers(CostModifiers.All);
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost }) : Message.Raw($"{cost}"));
            }
        }

        if (view.HasStarCostX)
            parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_X_STARS") : Message.Raw("X"));
        else if (view.CurrentStarCost >= 0)
        {
            var starCost = view.StarCostWithModifiers;
            parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = starCost }) : Message.Raw($"{starCost}"));
        }

        return parts.Count > 0 ? Message.Join(", ", parts.ToArray()) : null;
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
