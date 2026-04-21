using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[ElementSettingsKey("potion")]
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    // Compendium/lab insertion point — ProxyPotionLabHolder yields Status
    // (locked / undiscovered). Regular potion-holder instances don't yield it.
    typeof(StatusAnnouncement),
    // Shop-context insertion points — ProxyPotionHolder never yields these, but
    // ProxyMerchantSlot does, and potion's order positions them here.
    typeof(PriceAnnouncement),
    typeof(SoldOutAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyPotionHolder : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = GetView();
        if (view == null) yield break;

        if (view.IsEmptySlot)
        {
            yield return new LabelAnnouncement(Message.Localized("ui", "LABELS.EMPTY_POTION_SLOT"));
            yield return new TypeAnnouncement("potion");
            yield break;
        }

        if (view.Title != null)
            yield return new LabelAnnouncement(view.Title);
        yield return new TypeAnnouncement("potion");

        if (!string.IsNullOrEmpty(view.Description))
            yield return new TooltipAnnouncement(view.Description);
    }

    private readonly PotionModel? _model;

    public ProxyPotionHolder(Control control) : base(control) { }

    private ProxyPotionHolder(PotionModel model) : base()
    {
        _model = model;
    }

    public static ProxyPotionHolder FromModel(PotionModel model) => new(model);

    private PotionView? GetView() =>
        _model != null ? PotionView.FromModel(_model) : PotionView.FromControl(Control);

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null) return null;
        if (view.IsEmptySlot) return Message.Localized("ui", "LABELS.EMPTY_POTION_SLOT");
        return view.Title != null ? Message.Raw(view.Title) : null;
    }

    public override string? GetTypeKey() => "potion";

    public override Message? GetTooltip()
    {
        var desc = GetView()?.Description;
        return string.IsNullOrEmpty(desc) ? null : Message.Raw(desc);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null) return base.HandleBuffers(buffers);

        if (view.IsEmptySlot)
        {
            var uiBuffer = buffers.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                uiBuffer.Add("Empty potion slot");
                var slotDesc = new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "POTION_SLOT.description").GetFormattedText();
                if (!string.IsNullOrEmpty(slotDesc))
                    uiBuffer.Add(StripBbcode(slotDesc));
                buffers.EnableBuffer("ui", true);
            }
            return "ui";
        }

        if (view.Model == null) return base.HandleBuffers(buffers);

        var potionBuffer = buffers.GetBuffer("potion") as PotionBuffer;
        if (potionBuffer != null)
        {
            potionBuffer.Bind(view.Model);
            potionBuffer.Update();
            buffers.EnableBuffer("potion", true);
        }

        return "potion";
    }
}
