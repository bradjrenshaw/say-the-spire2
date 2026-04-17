using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class ProxyCardViewSortButton : ProxyElement
{
    public ProxyCardViewSortButton(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null)
            return Message.Raw(OverrideLabel);

        if (Control is NCardViewSortButton button)
        {
            var text = FindChildText(button.GetNodeOrNull("Label") ?? button) ?? CleanNodeName(button.Name);
            return Message.Raw(text);
        }

        return Message.Raw(CleanNodeName(Control!.Name));
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        if (Control is not NCardViewSortButton button)
            return null;

        return Message.Localized("ui", button.IsDescending ? "SORT.DESCENDING" : "SORT.ASCENDING");
    }
}
