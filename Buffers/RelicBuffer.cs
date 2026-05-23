using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.UI.Elements;
namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(HeaderAnnouncement),
    typeof(DescriptionAnnouncement),
    typeof(RelicCounterAnnouncement),
    typeof(RelicDisabledAnnouncement),
    typeof(HoverTipsAnnouncement),
    typeof(ExtrasAnnouncement)
)]
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
        var attrOrder = typeof(RelicBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "relic", attrOrder, BuildAnnouncements(model, extraLines));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(RelicModel model, IEnumerable<string>? extraLines)
    {
        yield return new HeaderAnnouncement(BuildHeader(model));

        var desc = BuildDescription(model);
        if (!string.IsNullOrEmpty(desc))
            yield return new DescriptionAnnouncement(desc);

        if (model.ShowCounter && model.DisplayAmount != 0)
            yield return new RelicCounterAnnouncement(model.DisplayAmount);

        if (model.Status == RelicStatus.Disabled)
            yield return new RelicDisabledAnnouncement();

        IEnumerable<IHoverTip> tips = Array.Empty<IHoverTip>();
        try { tips = model.HoverTips.OfType<IHoverTip>().ToList(); }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Relic hover tips access failed: {e.Message}"); }
        // skipFirst: relics' first hover tip is the relic itself, which
        // would duplicate the header / description.
        yield return new HoverTipsAnnouncement(tips, skipFirst: true);

        if (extraLines != null)
            yield return new ExtrasAnnouncement(extraLines);
    }

    private static string BuildHeader(RelicModel model)
    {
        var title = model.Title.GetFormattedText();
        return model.Rarity != RelicRarity.None
            ? $"{title}, {model.Rarity}"
            : title;
    }

    private static string? BuildDescription(RelicModel model)
    {
        try
        {
            var desc = model.DynamicDescription.GetFormattedText();
            return string.IsNullOrEmpty(desc) ? null : ProxyElement.StripBbcode(desc);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Relic description access failed: {e.Message}"); return null; }
    }
}
