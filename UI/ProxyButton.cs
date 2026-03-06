using Godot;

namespace Sts2AccessibilityMod.UI;

public class ProxyButton : ProxyElement
{
    public ProxyButton(Control control) : base(control) { }

    public override string? GetLabel()
    {
        if (OverrideLabel != null) return OverrideLabel;
        var text = FindChildText(Control) ?? FindSiblingLabel(Control);
        if (text != null) return text;
        // Skip auto-generated Godot names like @Control@1384
        var name = Control.Name.ToString();
        if (name.StartsWith('@')) return null;
        return CleanNodeName(name);
    }

    public override string? GetTypeKey() => "button";
}
