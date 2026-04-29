using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyBadge : ProxyElement
{
    private readonly SerializableBadge? _badge;

    public ProxyBadge(Control control, SerializableBadge? badge = null) : base(control)
    {
        _badge = badge;
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var loc = GetLocString("Title");
        return loc != null ? Message.Raw(loc.GetFormattedText()) : Message.Raw(CleanNodeName(Control?.Name.ToString() ?? "Badge"));
    }

    public override string? GetTypeKey() => "badge";

    public override Message? GetTooltip()
    {
        var loc = GetLocString("Description");
        return loc != null ? Message.Raw(StripBbcode(loc.GetFormattedText())) : null;
    }

    private LocString? GetLocString(string suffix)
    {
        if (_badge == null || string.IsNullOrWhiteSpace(_badge.Id))
            return null;

        var rarityKey = $"{_badge.Id}.{GetRarityPrefix(_badge.Rarity)}{suffix}";
        if (LocString.Exists("badges", rarityKey))
            return new LocString("badges", rarityKey);

        var key = $"{_badge.Id}.{suffix.ToLowerInvariant()}";
        return LocString.Exists("badges", key) ? new LocString("badges", key) : null;
    }

    private static string GetRarityPrefix(BadgeRarity rarity) => rarity switch
    {
        BadgeRarity.Bronze => "bronze",
        BadgeRarity.Silver => "silver",
        BadgeRarity.Gold => "gold",
        _ => "ERROR",
    };
}
