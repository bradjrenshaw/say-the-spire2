using Godot;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyButton : ProxyElement
{
    public ProxyButton(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null) return Message.Raw(OverrideLabel);
        var text = FindChildText(Control) ?? FindSiblingLabel(Control);
        if (text != null) return Message.Raw(text);
        // Skip auto-generated Godot names like @Control@1384
        var name = Control.Name.ToString();
        if (name.StartsWith('@')) return null;
        return Message.Raw(CleanNodeName(name));
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        // Check if this is a disabled NClickableControl (locked button)
        if (Control is MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl ncc && !ncc.IsEnabled)
            return Message.Raw(LocalizationManager.GetOrDefault("ui", "LABELS.LOCKED", "Locked"));
        return null;
    }

    public override Message? GetTooltip()
    {
        // Look for a Description child (e.g., NSubmenuButton has %Description)
        var desc = Control.GetNodeOrNull<RichTextLabel>("%Description");
        if (desc != null)
        {
            var text = desc.Text;
            if (!string.IsNullOrEmpty(text))
                return Message.Raw(StripBbcode(text));
        }
        return null;
    }
}
