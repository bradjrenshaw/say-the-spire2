using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Sts2AccessibilityMod.UI;

public class ProxyPaginator : ProxyElement
{
    public ProxyPaginator(Control control) : base(control) { }

    public override string? GetLabel()
    {
        return OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "slider";

    public override string? GetStatusString()
    {
        // The paginator's %Label child shows the current option
        var labelNode = Control.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return text;
        }
        return null;
    }
}
