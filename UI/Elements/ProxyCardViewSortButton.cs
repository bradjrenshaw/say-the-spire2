using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardViewSortButton : ProxyElement
{
    public ProxyCardViewSortButton(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null)
            return Message.Raw(OverrideLabel);

        if (Control is NCardViewSortButton button)
        {
            var text = FindChildText(button.GetNodeOrNull("Label") ?? button) ?? CleanNodeName(button.Name);
            return Message.Raw(text);
        }

        return Message.Raw(CleanNodeName(Control!.Name));
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        if (Control is not NCardViewSortButton button)
            return null;

        return Message.Raw(button.IsDescending ? "Descending" : "Ascending");
    }
}
