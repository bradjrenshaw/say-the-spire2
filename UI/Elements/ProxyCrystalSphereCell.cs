using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement)
)]
public class ProxyCrystalSphereCell : ProxyElement
{
    public ProxyCrystalSphereCell(Control control) : base(control) { }

    private NCrystalSphereCell? Cell => Control as NCrystalSphereCell;

    private CrystalSphereCell? Entity => Cell?.Entity;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
    }

    public override Message? GetLabel()
    {
        var entity = Entity;
        if (entity == null) return null;

        if (entity.IsHidden)
            return Message.Localized("ui", "LABELS.HIDDEN");

        var item = entity.Item;
        if (item == null)
            return Message.Localized("ui", "LABELS.EMPTY");

        var itemLabel = GetItemLabel(item);
        var rangeStr = GetItemRangeString(item);
        return rangeStr != null ? Message.Raw($"{itemLabel} {rangeStr}") : Message.Raw(itemLabel);
    }

    private static string GetItemLabel(CrystalSphereItem item)
    {
        return item switch
        {
            CrystalSphereRelic => "Relic",
            CrystalSpherePotion => "Potion",
            CrystalSphereCardReward => "Card Reward",
            CrystalSphereGold => "Gold",
            CrystalSphereCurse => "Curse",
            _ => "Item"
        };
    }

    private static string? GetItemRangeString(CrystalSphereItem item)
    {
        if (item.Size.X <= 1 && item.Size.Y <= 1)
            return null;

        // Match GridContainer output: X, Y
        int startX = item.Position.X + 1;
        int startY = item.Position.Y + 1;
        int endX = item.Position.X + item.Size.X;
        int endY = item.Position.Y + item.Size.Y;
        return $"from {startX}, {startY} to {endX}, {endY}";
    }
}
