using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

// [AnnouncementOrder] used for entries with no inner proxy: card removal and
// the Blitz shop's offers (title + description, no price — they're free picks).
// When wrapping a card/relic/potion the composer uses the inner's order via
// AnnouncementOrderType below.
[ElementSettingsKey("shop_item")]
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(TooltipAnnouncement),
    typeof(PriceAnnouncement)
)]
public class ProxyMerchantSlot : ProxyElement
{
    public override System.Type AnnouncementOrderType =>
        GetInnerProxy()?.GetType() ?? typeof(ProxyMerchantSlot);

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var entry = GetEntry();
        if (entry == null) yield break;

        // Card removal: no inner proxy, yield our own label + type
        if (entry is MerchantCardRemovalEntry)
        {
            yield return new LabelAnnouncement(Message.Localized("ui", "LABELS.CARD_REMOVAL"));
            yield return new TypeAnnouncement("shop_item");
            if (!entry.IsStocked)
            {
                yield return PriceAnnouncement.SoldOut();
                yield break;
            }
            yield return new PriceAnnouncement(entry.Cost, canAfford: entry.EnoughGold);
            yield break;
        }

        // Blitz offer: no inner proxy and no price. Used and disabled offers
        // can't take focus, so there is no sold-out state to report.
        if (BlitzMerchantEntryView.FromEntry(entry) is { } blitz)
        {
            yield return new LabelAnnouncement(Message.Raw(blitz.Title));
            yield return new TypeAnnouncement("shop_item");
            yield return new TooltipAnnouncement(Message.Raw(StripBbcode(blitz.Description)));
            yield break;
        }

        // Standard entry: flatten inner's announcements and append shop info.
        // The inner's [AnnouncementOrder] (via AnnouncementOrderType) positions
        // PriceAnnouncement at its declared insertion point.
        var inner = GetInnerProxy();
        if (inner != null)
            foreach (var a in inner.GetFocusAnnouncements())
                yield return a;
        else if (Control != null)
            yield return new LabelAnnouncement(CleanNodeName(Control.Name));

        if (!entry.IsStocked)
        {
            yield return PriceAnnouncement.SoldOut();
            yield break;
        }

        var isOnSale = entry is MerchantCardEntry cardEntry && cardEntry.IsOnSale;
        yield return new PriceAnnouncement(entry.Cost, canAfford: entry.EnoughGold, isOnSale: isOnSale);
    }

    private UIElement? _innerProxy;
    private MerchantEntry? _cachedEntry;

    public ProxyMerchantSlot(Control control) : base(control) { }

    private NMerchantSlot? Slot => Control as NMerchantSlot;

    private MerchantEntry? GetEntry()
    {
        try { return Slot?.Entry; }
        catch (System.Exception e) { MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] MerchantSlot.Entry access failed: {e.Message}"); return null; }
    }

    private UIElement? GetInnerProxy()
    {
        var entry = GetEntry();
        if (entry == _cachedEntry && _innerProxy != null)
            return _innerProxy;

        _cachedEntry = entry;
        _innerProxy = entry switch
        {
            MerchantCardEntry ce when ce.CreationResult?.Card != null =>
                ProxyCard.FromModel(ce.CreationResult.Card),
            MerchantRelicEntry re when re.Model != null =>
                ProxyRelicHolder.FromModel(re.Model),
            MerchantPotionEntry pe when pe.Model != null =>
                ProxyPotionHolder.FromModel(pe.Model),
            _ => null
        };
        return _innerProxy;
    }

    public override Message? GetLabel()
    {
        var inner = GetInnerProxy();
        if (inner != null) return inner.GetLabel();

        var entry = GetEntry();
        if (entry is MerchantCardRemovalEntry)
            return Message.Localized("ui", "LABELS.CARD_REMOVAL");
        if (BlitzMerchantEntryView.FromEntry(entry) is { } blitz)
            return Message.Raw(blitz.Title);

        return Message.Raw(CleanNodeName(Control!.Name));
    }

    public override Message? GetTooltip() =>
        BlitzMerchantEntryView.FromEntry(GetEntry()) is { } blitz
            ? Message.Raw(StripBbcode(blitz.Description))
            : null;

    public override string? GetTypeKey()
    {
        var inner = GetInnerProxy();
        if (inner != null) return inner.GetTypeKey();

        return "shop_item";
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entry = GetEntry();
        if (entry == null) return base.HandleBuffers(buffers);

        // Card removal has no inner proxy
        if (entry is MerchantCardRemovalEntry removalEntry)
            return HandleRemovalBuffers(buffers, removalEntry);

        if (BlitzMerchantEntryView.FromEntry(entry) is { } blitz)
            return HandleBlitzBuffers(buffers, blitz);

        var inner = GetInnerProxy();
        if (inner == null) return base.HandleBuffers(buffers);

        // Delegate to inner proxy for standard buffer population
        var result = inner.HandleBuffers(buffers);

        // Append merchant-specific info (price, sale) to whichever buffer the inner proxy used
        var bufferKey = result ?? "ui";
        var buffer = buffers.GetBuffer(bufferKey);
        if (buffer != null)
        {
            buffer.Add(Message.Localized("ui", "RESOURCE.PRICE", new { cost = entry.Cost }).Resolve());
            if (entry is MerchantCardEntry cardEntry && cardEntry.IsOnSale)
                buffer.Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.ON_SALE", "On sale"));
        }

        return result;
    }

    private static string? HandleBlitzBuffers(BufferManager buffers, BlitzMerchantEntryView blitz)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            uiBuffer.Add(blitz.Title);
            uiBuffer.Add(StripBbcode(blitz.Description));
            // The first tip repeats the title and description above; the rest
            // explain what the offer references (e.g. its enchantment).
            BufferAnnouncementComposer.Compose(uiBuffer,
                new[] { new HoverTipsAnnouncement(blitz.HoverTips, skipFirst: true) });
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
            uiBuffer.Add(Message.Localized("ui", "RESOURCE.PRICE", new { cost = removalEntry.Cost }).Resolve());
            if (!removalEntry.IsStocked)
                uiBuffer.Add("Already used");
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
