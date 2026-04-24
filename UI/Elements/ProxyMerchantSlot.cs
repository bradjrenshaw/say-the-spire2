using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

// [AnnouncementOrder] used only for the card-removal fallback (no inner proxy).
// When wrapping a card/relic/potion the composer uses the inner's order via
// AnnouncementOrderType below.
[ElementSettingsKey("shop_item")]
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(PriceAnnouncement),
    typeof(SoldOutAnnouncement)
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
                yield return new SoldOutAnnouncement();
                yield break;
            }
            yield return new PriceAnnouncement(entry.Cost, canAfford: entry.EnoughGold);
            yield break;
        }

        // Standard entry: flatten inner's announcements and append shop info.
        // The inner's [AnnouncementOrder] (via AnnouncementOrderType) positions
        // PriceAnnouncement / SoldOutAnnouncement at its declared insertion points.
        var inner = GetInnerProxy();
        if (inner != null)
            foreach (var a in inner.GetFocusAnnouncements())
                yield return a;
        else if (Control != null)
            yield return new LabelAnnouncement(CleanNodeName(Control.Name));

        if (!entry.IsStocked)
        {
            yield return new SoldOutAnnouncement();
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

        return Message.Raw(CleanNodeName(Control!.Name));
    }

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
