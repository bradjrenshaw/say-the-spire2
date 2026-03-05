using Godot;

namespace Sts2AccessibilityMod.UI;

public class ProxyDropdown : ProxyElement
{
    public ProxyDropdown(Control control) : base(control) { }

    public override string? GetLabel()
    {
        return OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetStatusString()
    {
        // The dropdown's selected value is in %Label or a child text node
        var labelNode = Control.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return text;
        }

        return FindChildText(Control);
    }

    public override string? GetTypeKey() => "dropdown";
}
