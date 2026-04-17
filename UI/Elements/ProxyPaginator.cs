using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class ProxyPaginator : ProxyElement
{
    public ProxyPaginator(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("slider");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (Control == null) return null;
        var text = OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "slider";

    public override Message? GetStatusString()
    {
        // The paginator's %Label child shows the current option
        var labelNode = Control?.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return Message.Raw(text);
        }
        return null;
    }
}
