using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

// Delegates settings / [AnnouncementOrder] to ProxyButton — cell is perceived
// as a button; range info rides along as StatusAnnouncement.
public class ProxyCrystalSphereCell : ProxyElement
{
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyCrystalSphereCell(Control control) : base(control) { }

    private NCrystalSphereCell? Cell => Control as NCrystalSphereCell;

    private CrystalSphereCell? Entity => Cell?.Entity;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
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

        return Message.Raw(GetItemLabel(item));
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var item = Entity?.Item;
        if (item == null) return null;

        var range = GetItemRangeString(item);
        return range != null ? Message.Raw(range) : null;
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
