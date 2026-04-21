using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ProxyPaginator : ProxyElement
{
    // User-perceives this as a slider; share settings / [AnnouncementOrder] with ProxySlider.
    public override System.Type AnnouncementOrderType => typeof(ProxySlider);

    public ProxyPaginator(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("slider");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
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
