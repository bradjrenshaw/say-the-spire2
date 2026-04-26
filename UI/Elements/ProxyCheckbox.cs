using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
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
        bool? isChecked = Control switch
        {
            NTickbox t => t.IsTicked,
            NCardTypeTickbox t => t.IsTicked,
            NCardCostTickbox t => t.IsTicked,
            _ => null,
        };
        if (!isChecked.HasValue) return null;
        return Message.Localized("ui", isChecked.Value ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED");
    }

    protected override void OnFocus()
    {
        switch (Control)
        {
            case NTickbox tickbox:
                tickbox.Toggled += OnTickboxToggled;
                break;
            case NCardTypeTickbox cardType:
                cardType.Toggled += OnCardTypeToggled;
                break;
            case NCardCostTickbox cost:
                // NCardCostTickbox lacks a Toggled event; fall back to Released
                // and defer one frame because IsTicked isn't updated yet on the
                // released frame.
                cost.Released += OnReleased;
                break;
        }
    }

    protected override void OnUnfocus()
    {
        switch (Control)
        {
            case NTickbox tickbox:
                tickbox.Toggled -= OnTickboxToggled;
                break;
            case NCardTypeTickbox cardType:
                cardType.Toggled -= OnCardTypeToggled;
                break;
            case NCardCostTickbox cost:
                cost.Released -= OnReleased;
                break;
        }
    }

    private void OnTickboxToggled(NTickbox _) => OutputStatus();

    private void OnCardTypeToggled(NCardTypeTickbox _) => OutputStatus();

    private void OnReleased(NClickableControl _)
    {
        var tree = Control?.GetTree();
        if (tree != null)
        {
            tree.CreateTimer(0).Timeout += OutputStatus;
            return;
        }
        OutputStatus();
    }

    private void OutputStatus()
    {
        var status = GetStatusString();
        if (status != null)
            SpeechManager.Output(status);
    }
}
