using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Buffers;

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
        catch { return null; }
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

    public override string? GetLabel()
    {
        var inner = GetInnerProxy();
        if (inner != null) return inner.GetLabel();

        var entry = GetEntry();
        if (entry is MerchantCardRemovalEntry)
            return "Card Removal";

        return CleanNodeName(Control!.Name);
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

    public override string? GetExtrasString()
    {
        return GetInnerProxy()?.GetExtrasString();
    }

    public override string? GetTooltip()
    {
        return GetInnerProxy()?.GetTooltip();
    }

    public override string? GetStatusString()
    {
        var entry = GetEntry();
        if (entry == null) return null;

        if (!entry.IsStocked) return "Sold out";

        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"{entry.Cost} gold");

        if (!entry.EnoughGold)
            parts.Add("Not enough gold");

        if (entry is MerchantCardEntry cardEntry && cardEntry.IsOnSale)
            parts.Add("On sale");

        return string.Join(", ", parts);
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
            buffer.Add($"Price: {entry.Cost} gold");
            if (entry is MerchantCardEntry cardEntry && cardEntry.IsOnSale)
                buffer.Add("On sale!");
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
            uiBuffer.Add($"Price: {removalEntry.Cost} gold");
            if (!removalEntry.IsStocked)
                uiBuffer.Add("Already used");
            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
