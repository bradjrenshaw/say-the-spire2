using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardViewSortButton : ProxyElement
{
    // User-perceives this as a button; share settings / [AnnouncementOrder] with ProxyButton.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyCardViewSortButton(Control control) : base(control) { }

    protected override void OnFocus()
    {
        if (Control is NCardViewSortButton button)
            button.Released += OnReleased;
    }

    protected override void OnUnfocus()
    {
        if (Control is NCardViewSortButton button)
            button.Released -= OnReleased;
    }

    private void OnReleased(NClickableControl control)
    {
        var status = GetStatusString();
        if (status != null)
            SpeechManager.Output(status);
    }

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
