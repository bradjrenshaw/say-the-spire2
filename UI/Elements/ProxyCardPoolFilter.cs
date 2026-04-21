using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardPoolFilter : ProxyElement
{
    // User-perceives this as a checkbox; share settings / [AnnouncementOrder] with ProxyCheckbox.
    public override System.Type AnnouncementOrderType => typeof(ProxyCheckbox);

    public ProxyCardPoolFilter(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("checkbox");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null)
            return Message.Raw(OverrideLabel);

        if (Control == null) return null;
        var text = FindChildText(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "checkbox";

    public override Message? GetStatusString()
    {
        if (Control is not NCardPoolFilter filter)
            return null;

        var key = filter.IsSelected ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED";
        var text = LocalizationManager.Get("ui", key);
        return text != null ? Message.Raw(text) : null;
    }

    public override Message? GetTooltip()
    {
        if (Control is NCardPoolFilter filter && filter.Loc != null)
            return Message.Raw(filter.Loc.GetFormattedText());

        return null;
    }
}
