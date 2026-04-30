using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;
namespace SayTheSpire2.Buffers;

public class RelicBuffer : Buffer
{
    private RelicModel? _model;
    private IReadOnlyList<string> _extraLines = Array.Empty<string>();

    public RelicBuffer() : base("relic") { }

    public void Bind(RelicModel model)
    {
        _model = model;
        _extraLines = Array.Empty<string>();
    }

    public void Bind(RelicModel model, IEnumerable<string>? extraLines)
    {
        _model = model;
        _extraLines = extraLines?
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray()
            ?? Array.Empty<string>();
    }

    protected override void ClearBinding()
    {
        _model = null;
        _extraLines = Array.Empty<string>();
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(() => Populate(this, _model, _extraLines));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with relic data.
    /// Used by RelicBuffer.Update(), ProxyRelicHolder, merchant slots, reward buttons, etc.
    /// </summary>
    public static void Populate(Buffer buffer, RelicModel model, IEnumerable<string>? extraLines = null)
    {
        var title = model.Title.GetFormattedText();
        var rarity = model.Rarity;
        if (rarity != RelicRarity.None)
            buffer.Add($"{title}, {rarity}");
        else
            buffer.Add(title);

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            buffer.Add(ProxyElement.StripBbcode(desc));

        if (model.ShowCounter && model.DisplayAmount != 0)
            buffer.Add(Message.Localized("ui", "RELIC.COUNTER", new { amount = model.DisplayAmount }).Resolve());

        if (model.Status == RelicStatus.Disabled)
            buffer.Add(LocalizationManager.GetOrDefault("ui", "RELIC.DISABLED", "Disabled"));

        // Hover tips: skip first (it's the relic itself), rest are keywords/references
        try
        {
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is HoverTip hoverTip)
                {
                    var tipTitle = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(tipTitle) && !string.IsNullOrEmpty(tipDesc))
                        buffer.Add($"{tipTitle}: {ProxyElement.StripBbcode(tipDesc)}");
                    else if (!string.IsNullOrEmpty(tipTitle))
                        buffer.Add(tipTitle);
                }
            }
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Relic hover tips access failed: {e.Message}"); }

        if (extraLines == null)
            return;

        foreach (var line in extraLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                buffer.Add(line.Trim());
        }
    }

    /// <summary>
    /// Extracts CardHoverTips from the bound relic model, for cross-buffer population.
    /// </summary>
    public static List<CardHoverTip> GetCardTips(RelicModel model)
    {
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
        catch (Exception e) { Log.Error($"[AccessibilityMod] Relic card tips access failed: {e.Message}"); }
        return result;
    }
}
