using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ProxyRestSiteButton : ProxyElement
{
    // User-perceives this as a button; share settings / [AnnouncementOrder] with ProxyButton.
    // Minor cadence change: tooltip now reads after type rather than between
    // label and type. Small tradeoff for unified button settings.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyRestSiteButton(Control control) : base(control) { }

    private NRestSiteButton? Button => Control as NRestSiteButton;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var option = Button?.Option;
        if (option == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        yield return new LabelAnnouncement(option.Title.GetFormattedText());

        var desc = option.Description.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            yield return new TooltipAnnouncement(StripBbcode(desc));

        yield return new TypeAnnouncement("button");
    }

    public override Message? GetLabel()
    {
        var option = Button?.Option;
        if (option == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        return Message.Raw(option.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "button";
}
