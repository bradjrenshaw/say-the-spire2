using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(AscensionAnnouncement),
    typeof(AchievementsLockedAnnouncement)
)]
public class ProxyRunHistoryPlayerIcon : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var icon = Icon;
        if (icon == null)
            yield break;

        var ascensionLabel = AscensionLabelField?.GetValue(icon) as Label;
        if (ascensionLabel != null && !string.IsNullOrWhiteSpace(ascensionLabel.Text))
            yield return new AscensionAnnouncement(ascensionLabel.Text.Trim());

        var achievementLock = AchievementLockField?.GetValue(icon) as Control;
        if (achievementLock?.Visible == true)
            yield return new AchievementsLockedAnnouncement();
    }

    private static readonly FieldInfo? AscensionLabelField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_ascensionLabel");
    private static readonly FieldInfo? AchievementLockField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_achievementLock");
    private static readonly FieldInfo? HoverTipsField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_hoverTips");

    public ProxyRunHistoryPlayerIcon(Control control) : base(control) { }

    private NRunHistoryPlayerIcon? Icon => Control as NRunHistoryPlayerIcon;

    public override Message? GetLabel()
    {
        var icon = Icon;
        if (icon == null)
            return null;

        return Message.Raw(ModelDb.GetById<CharacterModel>(icon.Player.Character).Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var icon = Icon;
        if (icon == null)
            return null;

        var parts = new List<string>();
        var ascensionLabel = AscensionLabelField?.GetValue(icon) as Label;
        if (ascensionLabel != null && !string.IsNullOrWhiteSpace(ascensionLabel.Text))
            parts.Add(Ui("RUN_HISTORY.ASCENSION", new { value = ascensionLabel.Text.Trim() }));

        var achievementLock = AchievementLockField?.GetValue(icon) as Control;
        if (achievementLock?.Visible == true)
            parts.Add(Ui("RUN_HISTORY.ACHIEVEMENTS_LOCKED"));

        return parts.Count > 0 ? Message.Raw(string.Join(", ", parts)) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null)
            return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel()?.Resolve();
        if (!string.IsNullOrWhiteSpace(label))
            uiBuffer.Add(label);

        var status = GetStatusString()?.Resolve();
        if (!string.IsNullOrWhiteSpace(status))
            uiBuffer.Add(status);

        foreach (var detail in GetExpandedDetailItems())
            uiBuffer.Add(detail);

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    public string? GetExpandedDetails()
    {
        var parts = GetExpandedDetailItems();
        return parts.Count > 0 ? string.Join(". ", parts) : null;
    }

    private List<string> GetExpandedDetailItems()
    {
        var icon = Icon;
        if (icon == null)
            return new List<string>();

        var parts = new List<string>();
        var hoverTips = HoverTipsField?.GetValue(icon) as IEnumerable<IHoverTip>;
        if (hoverTips != null)
        {
            foreach (var tip in hoverTips)
            {
                if (tip is HoverTip hoverTip)
                {
                    if (!string.IsNullOrWhiteSpace(hoverTip.Title))
                        parts.Add(hoverTip.Title.Trim());
                    if (!string.IsNullOrWhiteSpace(hoverTip.Description))
                        parts.Add(ProxyElement.StripBbcode(hoverTip.Description).Replace('\n', ' ').Trim());
                }
            }
        }

        return parts;
    }

    private static string Ui(string key, object vars)
    {
        return Message.Localized("ui", key, vars).Resolve();
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
