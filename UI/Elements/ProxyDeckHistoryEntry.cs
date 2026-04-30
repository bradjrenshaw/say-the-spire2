using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(EnergyCostAnnouncement),
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
    private CardView? GetView()
    {
        var card = Entry?.Card;
        return card == null ? null : CardView.FromModel(card);
    }
    private int Amount => AmountField?.GetValue(Entry) as int? ?? 1;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = GetView();
        if (view == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        // Label uses GetLabel since it folds in quantity + modifiers
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        int? energyCost = null;
        bool energyIsX = false;
        if (view.EnergyCost != null)
        {
            if (view.EnergyCost.CostsX) { energyCost = 0; energyIsX = true; }
            else energyCost = view.EnergyCost.GetWithModifiers(CostModifiers.All);
        }

        int? starCost = null;
        bool starIsX = false;
        if (view.HasStarCostX) { starCost = 0; starIsX = true; }
        else if (view.CurrentStarCost >= 0)
            starCost = view.StarCostWithModifiers;

        if (energyCost.HasValue || starCost.HasValue)
            yield return new EnergyCostAnnouncement(energyCost, energyIsX, starCost, starIsX);

        yield return new SubtypeAnnouncement(view.TypeKey);
        yield return new TypeAnnouncement("card");

        if (!string.IsNullOrEmpty(view.Description))
            yield return new TooltipAnnouncement(view.Description);
    }

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        var title = view.Title;
        var modifiers = new List<string>();
        if (view.ReplayCount > 0)
            modifiers.Add(Message.Localized("ui", "MODIFIER.REPLAY", new { count = view.ReplayCount }).Resolve());
        if (!string.IsNullOrEmpty(view.EnchantmentTitle))
            modifiers.Add(view.EnchantmentTitle!);
        if (!string.IsNullOrEmpty(view.AfflictionTitle))
            modifiers.Add(view.AfflictionTitle!);
        if (modifiers.Count > 0)
            title = $"{title} ({string.Join(", ", modifiers)})";

        return Amount > 1 ? Message.Localized("ui", "CARD.QUANTITY", new { amount = Amount, title }) : Message.Raw(title);
    }

    public override string? GetTypeKey() => "card";

    public override Message? GetTooltip()
    {
        var view = GetView();
        if (view == null) return null;
        return string.IsNullOrEmpty(view.Description) ? null : Message.Raw(view.Description);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null)
            return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card") as CardBuffer;
        if (cardBuffer != null)
        {
            var floorText = RunHistoryAcquisitionText.FromFloors(Entry?.FloorsAddedToDeck);
            cardBuffer.Bind(view.DisplayedModel, floorText != null ? new[] { floorText } : null);
            cardBuffer.Update();
            buffers.EnableBuffer("card", true);
        }

        var upgradeBuffer = buffers.GetBuffer("upgrade") as UpgradeBuffer;
        if (upgradeBuffer != null)
        {
            upgradeBuffer.Bind(view.DisplayedModel);
            upgradeBuffer.Update();
            buffers.EnableBuffer("upgrade", true);
        }

        return "card";
    }
}
