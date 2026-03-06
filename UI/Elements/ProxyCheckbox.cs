using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxyCheckbox : ProxyElement
{
    public ProxyCheckbox(Control control) : base(control) { }

    public override string? GetLabel()
    {
        return OverrideLabel ?? FindChildText(Control) ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "checkbox";

    public override string? GetStatusString()
    {
        if (Control is NTickbox tickbox)
        {
            var key = tickbox.IsTicked ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED";
            return LocalizationManager.Get("ui", key);
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
        if (!string.IsNullOrEmpty(status))
            SpeechManager.Output(status);
    }
}
