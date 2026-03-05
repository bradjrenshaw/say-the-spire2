using Godot;

namespace Sts2AccessibilityMod.UI;

public class ProxyButton : ProxyElement
{
    public ProxyButton(Control control) : base(control) { }

    public override string? GetLabel()
    {
        return OverrideLabel ?? FindChildText(Control) ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "button";
}
