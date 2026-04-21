using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyCheckbox : ProxyElement
{
    public ProxyCheckbox(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("checkbox");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (Control == null) return null;
        var text = OverrideLabel ?? FindChildText(Control) ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "checkbox";

    public override Message? GetStatusString()
    {
        if (Control is NTickbox tickbox)
        {
            var key = tickbox.IsTicked ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED";
            var text = LocalizationManager.Get("ui", key);
            return text != null ? Message.Raw(text) : null;
        }
        return null;
    }

    protected override void OnFocus()
    {
        if (Control is NTickbox tickbox)
            tickbox.Toggled += OnToggled;
    }

    protected override void OnUnfocus()
    {
        if (Control is NTickbox tickbox)
            tickbox.Toggled -= OnToggled;
    }

    private void OnToggled(NTickbox tickbox)
    {
        var status = GetStatusString();
        if (status != null)
            SpeechManager.Output(status);
    }
}
