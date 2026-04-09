using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyMerchantSlot : ProxyElement
{
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

        return "shop item";
    }

    public override string? GetSubtypeKey()
    {
        return GetInnerProxy()?.GetSubtypeKey();
    }

    public override Message? GetExtrasString()
    {
        return GetInnerProxy()?.GetExtrasString();
    }

    public override Message? GetTooltip()
    {
        return GetInnerProxy()?.GetTooltip();
    }

    public override Message? GetStatusString()
    {
        var entry = GetEntry();
        if (entry == null) return null;

        if (!entry.IsStocked) return Message.Localized("ui", "LABELS.SOLD_OUT");

        var parts = new System.Collections.Generic.List<string>();
        parts.Add(Message.Localized("ui", "RESOURCE.PRICE", new { cost = entry.Cost }).Resolve());

        if (!entry.EnoughGold)
            parts.Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.NOT_ENOUGH_GOLD", "Not enough gold"));

        if (entry is MerchantCardEntry cardEntry && cardEntry.IsOnSale)
            parts.Add(LocalizationManager.GetOrDefault("ui", "RESOURCE.ON_SALE", "On sale"));

        return Message.Raw(string.Join(", ", parts));
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
