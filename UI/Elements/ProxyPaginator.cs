using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyPaginator : ProxyElement
{
    public ProxyPaginator(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        var text = OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "slider";

    public override Message? GetStatusString()
    {
        // The paginator's %Label child shows the current option
        var labelNode = Control.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return Message.Raw(text);
        }
        return null;
    }
}
