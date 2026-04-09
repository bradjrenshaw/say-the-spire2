using Godot;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyDropdown : ProxyElement
{
    public ProxyDropdown(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        var text = OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override Message? GetStatusString()
    {
        // The dropdown's selected value is in %Label or a child text node
        var labelNode = Control.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return Message.Raw(text);
        }

        var childText = FindChildText(Control);
        return childText != null ? Message.Raw(childText) : null;
    }

    public override string? GetTypeKey() => "dropdown";
}
