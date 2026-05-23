using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Potions;
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
    typeof(HoverTipsAnnouncement)
)]
public class PotionBuffer : Buffer
{
    private PotionModel? _model;

    public PotionBuffer() : base("potion") { }

    public void Bind(PotionModel model)
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
        Repopulate(() => Populate(this, _model));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with potion data.
    /// Runs through <see cref="BufferAnnouncementComposer"/> so the user's
    /// per-buffer settings (reorder, toggle individual entries) take effect
    /// here exactly like they do on the card buffer.
    /// </summary>
    public static void Populate(Buffer buffer, PotionModel model)
    {
        var attrOrder = typeof(PotionBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "potion", attrOrder, BuildAnnouncements(model));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(PotionModel model)
    {
        yield return new HeaderAnnouncement(BuildHeader(model));

        var desc = BuildDescription(model);
        if (!string.IsNullOrEmpty(desc))
            yield return new DescriptionAnnouncement(desc);

        IEnumerable<IHoverTip> tips = Array.Empty<IHoverTip>();
        try { tips = model.HoverTips.OfType<IHoverTip>().ToList(); }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Potion hover tips access failed: {e.Message}"); }
        // skipFirst: the potion model's first hover tip is the potion itself,
        // which would duplicate the header / description.
        yield return new HoverTipsAnnouncement(tips, skipFirst: true);
    }

    private static string BuildHeader(PotionModel model)
    {
        var title = model.Title.GetFormattedText();
        return model.Rarity != PotionRarity.None
            ? $"{title}, {model.Rarity}"
            : title;
    }

    private static string? BuildDescription(PotionModel model)
    {
        try
        {
            var desc = model.DynamicDescription.GetFormattedText();
            return string.IsNullOrEmpty(desc) ? null : ProxyElement.StripBbcode(desc);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Potion description access failed: {e.Message}"); return null; }
    }
}
