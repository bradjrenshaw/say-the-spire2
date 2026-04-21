using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[ElementSettingsKey("relic")]
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(RelicCounterAnnouncement),
    typeof(RelicDisabledAnnouncement),
    // Compendium insertion point — ProxyRelicCollectionEntry yields Status
    // (locked / undiscovered). Regular relic-holder instances don't yield it.
    typeof(StatusAnnouncement),
    // Shop-context insertion points — ProxyRelicHolder never yields these, but
    // ProxyMerchantSlot does, and relic's order positions them here.
    typeof(PriceAnnouncement),
    typeof(SoldOutAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyRelicHolder : ProxyElement
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
        yield return new TypeAnnouncement("relic");

        if (view.ShowCounter && view.DisplayAmount != 0)
            yield return new RelicCounterAnnouncement(view.DisplayAmount);

        if (view.IsDisabled)
            yield return new RelicDisabledAnnouncement();

        if (!string.IsNullOrEmpty(view.Description))
            yield return new TooltipAnnouncement(view.Description);
    }

    private readonly RelicModel? _model;

    public ProxyRelicHolder(Control control) : base(control) { }

    private ProxyRelicHolder(RelicModel model) : base()
    {
        _model = model;
    }

    public static ProxyRelicHolder FromModel(RelicModel model) => new(model);

    private RelicView? GetView() =>
        _model != null ? RelicView.FromModel(_model) : RelicView.FromControl(Control);

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(view.Title);
    }

    public override string? GetTypeKey() => "relic";

    public override Message? GetStatusString()
    {
        var view = GetView();
        if (view == null) return null;

        var parts = new List<string>();

        if (view.ShowCounter && view.DisplayAmount != 0)
            parts.Add(Message.Localized("ui", "RELIC.COUNTER", new { amount = view.DisplayAmount }).Resolve());

        if (view.IsDisabled)
            parts.Add(LocalizationManager.GetOrDefault("ui", "RELIC.DISABLED", "Disabled"));

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

        var relicBuffer = buffers.GetBuffer("relic") as RelicBuffer;
        if (relicBuffer != null)
        {
            relicBuffer.Bind(view.Model);
            relicBuffer.Update();
            buffers.EnableBuffer("relic", true);
        }

        // Populate card buffer if relic has card hover tips
        var cardTips = RelicBuffer.GetCardTips(view.Model);
        if (cardTips.Count > 0)
        {
            var cardBuffer = buffers.GetBuffer("card");
            if (cardBuffer != null)
            {
                cardBuffer.Clear();
                foreach (var cardTip in cardTips)
                {
                    if (cardBuffer.Count > 0)
                        cardBuffer.Add("---");
                    CardBuffer.Populate(cardBuffer, cardTip.Card);
                }
                buffers.EnableBuffer("card", true);
            }
        }

        return "relic";
    }
}
