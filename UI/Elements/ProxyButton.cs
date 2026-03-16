using Godot;

namespace SayTheSpire2.UI.Elements;

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

    public override string? GetStatusString()
    {
        // Check if this is a disabled NClickableControl (locked button)
        if (Control is MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl ncc && !ncc.IsEnabled)
            return "Locked";
        return null;
    }

    public override string? GetTooltip()
    {
        // Look for a Description child (e.g., NSubmenuButton has %Description)
        var desc = Control.GetNodeOrNull<RichTextLabel>("%Description");
        if (desc != null)
        {
            var text = desc.Text;
            if (!string.IsNullOrEmpty(text))
                return StripBbcode(text);
        }
        return null;
    }
}
