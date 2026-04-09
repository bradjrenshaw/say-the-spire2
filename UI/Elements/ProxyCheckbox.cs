using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxyCheckbox : ProxyElement
{
    public ProxyCheckbox(Control control) : base(control) { }

    public override Message? GetLabel()
    {
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
