using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context grouped announcement that expands a sequence of hover
/// tips (keyword tips + referenced-card tips) into one line per tip at its
/// slot in the order. Reordering or disabling this announcement moves /
/// hides the entire group as a unit; per-item control lives as settings
/// on this class (e.g. <c>card_tips_enabled</c>).
/// </summary>
[ShowInGlobalSettings]
public sealed class HoverTipsAnnouncement : Announcement
{
    private readonly IReadOnlyList<IHoverTip> _tips;
    private readonly bool _skipFirst;

    /// <param name="tips">Hover-tip enumeration from the model.</param>
    /// <param name="skipFirst">
    /// True if the first tip is the self-referential one (e.g. a relic's
    /// own keyword tip describing itself) that should be skipped. Card
    /// models don't have this; relics do.
    /// </param>
    public HoverTipsAnnouncement(IEnumerable<IHoverTip> tips, bool skipFirst = false)
    {
        _tips = new List<IHoverTip>(tips);
        _skipFirst = skipFirst;
    }

    public override string Key => "hover_tips";

    /// <summary>Focus-context: hover tips don't appear in the focus string. Empty.</summary>
    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        bool includeKeywordTips = ctx.ResolveBool(Key, "keyword_tips_enabled", true);
        bool includeCardTips = ctx.ResolveBool(Key, "card_tips_enabled", true);

        bool first = true;
        foreach (var tip in _tips)
        {
            if (first && _skipFirst) { first = false; continue; }
            first = false;

            if (tip is CardHoverTip cardTip)
            {
                if (!includeCardTips || cardTip.Card == null) continue;
                var formatted = CardBuffer.FormatHoverTip(cardTip.Card);
                if (!string.IsNullOrEmpty(formatted))
                    yield return Message.Raw(formatted);
            }
            else if (tip is HoverTip hoverTip)
            {
                if (!includeKeywordTips) continue;
                var title = hoverTip.Title;
                var desc = hoverTip.Description;
                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                    yield return Message.Raw($"{title}: {ProxyElement.StripBbcode(desc)}");
                else if (!string.IsNullOrEmpty(title))
                    yield return Message.Raw(title);
                else if (!string.IsNullOrEmpty(desc))
                    yield return Message.Raw(ProxyElement.StripBbcode(desc));
            }
        }
    }

    public static void RegisterSettings(Settings.CategorySetting category)
    {
        if (category.GetByKey("keyword_tips_enabled") == null)
            category.Add(new Settings.BoolSetting(
                "keyword_tips_enabled", "Include keyword hover tips", true,
                localizationKey: "SETTINGS.HOVER_TIPS.KEYWORD_TIPS_ENABLED"));
        if (category.GetByKey("card_tips_enabled") == null)
            category.Add(new Settings.BoolSetting(
                "card_tips_enabled", "Include referenced card hover tips", true,
                localizationKey: "SETTINGS.HOVER_TIPS.CARD_TIPS_ENABLED"));
    }
}
