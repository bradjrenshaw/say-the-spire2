using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context grouped announcement for a creature/player's powers. One
/// line per power. When verbose (default for the buffer), the power's first
/// hover-tip description is appended inline ("Strength 2: Add N to attack
/// damage") and any additional hover tips emit their own lines. When terse
/// (default for the Ctrl+P hotkey), each power renders as just name + amount
/// ("Strength 5", joined into "Strength 5, Vulnerable 4").
///
/// The "verbose" setting is Hotkey-scoped: the buffer always uses the full
/// rendering; only the hotkey readout honors the toggle. Default off for the
/// hotkey to match the pre-hotkey-announcement-system behavior.
/// </summary>
public sealed class PowersAnnouncement : Announcement
{
    private readonly IReadOnlyList<PowerModel> _powers;

    public PowersAnnouncement(IEnumerable<PowerModel> powers)
    {
        _powers = new List<PowerModel>(powers);
    }

    public override string Key => "powers";

    public static void RegisterSettings(CategorySetting category)
    {
        // Hotkey-only: only the Ctrl+P readout honors this. Buffer rendering
        // stays verbose unconditionally. Default off so the hotkey reads
        // tersely like it did before the hotkey-announcement system landed.
        category.Add(new BoolSetting("verbose", "Verbose", false, localizationKey: "SETTINGS.VERBOSE")
        {
            AllowedContexts = AnnouncementContexts.Hotkey
        });
    }

    /// <summary>Focus context: powers are surfaced separately, so the focus line is empty.</summary>
    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        // Buffer context always renders with descriptions; only the hotkey
        // readout consults the toggle.
        bool includeDescriptions = ctx.HotkeyKey == null
            || ctx.ResolveBool(Key, "verbose", false);

        foreach (var power in _powers)
        {
            foreach (var line in RenderPower(power, includeDescriptions))
                yield return line;
        }
    }

    private static IEnumerable<Message> RenderPower(PowerModel power, bool includeDescriptions)
    {
        var title = power.Title.GetFormattedText();
        var amount = power.DisplayAmount;
        var hasStacks = power.StackType == PowerStackType.Counter;
        var line = hasStacks && amount > 0 ? $"{title} {amount}" : title;

        if (!includeDescriptions)
        {
            yield return Message.Raw(line);
            yield break;
        }

        bool first = true;
        IEnumerable<IHoverTip>? tips = null;
        try { tips = power.HoverTips; }
        catch (Exception e) { Log.Info($"[AccessibilityMod] Power hover tip lookup failed: {e.Message}"); }

        if (tips == null)
        {
            yield return Message.Raw(line);
            yield break;
        }

        foreach (var tip in tips)
        {
            if (tip is HoverTip ht)
            {
                var desc = ht.Description;
                if (first)
                {
                    if (!string.IsNullOrEmpty(desc))
                        line += ": " + desc;
                    yield return Message.Raw(line);
                    first = false;
                }
                else
                {
                    var extraTitle = ht.Title;
                    var extraLine = !string.IsNullOrEmpty(extraTitle) && !string.IsNullOrEmpty(desc)
                        ? $"{extraTitle}: {desc}"
                        : !string.IsNullOrEmpty(extraTitle) ? extraTitle
                        : desc;
                    if (!string.IsNullOrEmpty(extraLine))
                        yield return Message.Raw(extraLine);
                }
            }
            else if (tip is CardHoverTip cardTip)
            {
                if (first)
                {
                    yield return Message.Raw(line);
                    first = false;
                }
                if (cardTip.Card != null)
                {
                    var formatted = CardBuffer.FormatHoverTip(cardTip.Card);
                    if (!string.IsNullOrEmpty(formatted))
                        yield return Message.Raw(formatted);
                }
            }
        }
        if (first)
            yield return Message.Raw(line);
    }
}
