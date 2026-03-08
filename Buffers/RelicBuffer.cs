using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
namespace SayTheSpire2.Buffers;

public class RelicBuffer : Buffer
{
    private RelicModel? _model;

    public RelicBuffer() : base("relic") { }

    public void Bind(RelicModel model)
    {
        _model = model;
    }

    protected override void ClearBinding()
    {
        _model = null;
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        var model = _model;
        if (model == null) return;

        Add(model.Title.GetFormattedText());

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            Add(desc);

        if (model.ShowCounter && model.DisplayAmount != 0)
            Add($"Counter: {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            Add("Disabled");

        // Hover tips: skip first (it's the relic itself), rest are keywords/references
        try
        {
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(tipDesc))
                        Add($"{title}: {tipDesc}");
                    else if (!string.IsNullOrEmpty(title))
                        Add(title);
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Gets the CardHoverTips from the bound relic model, for cross-buffer population.
    /// </summary>
    public IReadOnlyList<CardHoverTip> GetCardTips()
    {
        var model = _model;
        if (model == null) return [];

        var result = new List<CardHoverTip>();
        try
        {
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is CardHoverTip cardTip)
                    result.Add(cardTip);
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Populates any buffer with relic data. Used by other proxies (merchant, rewards)
    /// that write relic info into a shared buffer like "ui".
    /// </summary>
    public static void PopulateBuffer(Buffer buffer, RelicModel model, BufferManager? buffers = null)
    {
        buffer.Add(model.Title.GetFormattedText());

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            buffer.Add(desc);

        if (model.ShowCounter && model.DisplayAmount != 0)
            buffer.Add($"Counter: {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            buffer.Add("Disabled");

        try
        {
            var cardTips = new List<CardHoverTip>();
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is CardHoverTip cardTip)
                {
                    cardTips.Add(cardTip);
                }
                else if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(tipDesc))
                        buffer.Add($"{title}: {tipDesc}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }

            if (cardTips.Count > 0 && buffers != null)
            {
                var cardBuffer = buffers.GetBuffer("card");
                if (cardBuffer != null)
                {
                    cardBuffer.Clear();
                    foreach (var cardTip in cardTips)
                    {
                        if (cardBuffer.Count > 0)
                            cardBuffer.Add("---");
                        ProxyCard.PopulateCardBuffer(cardBuffer, cardTip.Card);
                    }
                    buffers.EnableBuffer("card", true);
                }
            }
        }
        catch { }
    }
}
