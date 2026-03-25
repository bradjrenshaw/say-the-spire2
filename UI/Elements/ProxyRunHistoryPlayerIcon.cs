using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace SayTheSpire2.UI.Elements;

public class ProxyRunHistoryPlayerIcon : ProxyElement
{
    private static readonly FieldInfo? AscensionLabelField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_ascensionLabel");
    private static readonly FieldInfo? AchievementLockField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_achievementLock");
    private static readonly FieldInfo? HoverTipsField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_hoverTips");

    public ProxyRunHistoryPlayerIcon(Control control) : base(control) { }

    private NRunHistoryPlayerIcon? Icon => Control as NRunHistoryPlayerIcon;

    public override string? GetLabel()
    {
        var icon = Icon;
        if (icon == null)
            return null;

        return ModelDb.GetById<CharacterModel>(icon.Player.Character).Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        var icon = Icon;
        if (icon == null)
            return null;

        var parts = new List<string>();
        var ascensionLabel = AscensionLabelField?.GetValue(icon) as Label;
        if (ascensionLabel != null && !string.IsNullOrWhiteSpace(ascensionLabel.Text))
            parts.Add($"Ascension {ascensionLabel.Text.Trim()}");

        var achievementLock = AchievementLockField?.GetValue(icon) as Control;
        if (achievementLock?.Visible == true)
            parts.Add("Achievements locked");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public string? GetExpandedDetails()
    {
        var icon = Icon;
        if (icon == null)
            return null;

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

        return parts.Count > 0 ? string.Join(". ", parts) : null;
    }
}
