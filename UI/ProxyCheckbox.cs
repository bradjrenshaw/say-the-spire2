using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2AccessibilityMod.Localization;

namespace Sts2AccessibilityMod.UI;

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
}
