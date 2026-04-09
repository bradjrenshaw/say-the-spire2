using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardPoolFilter : ProxyElement
{
    public ProxyCardPoolFilter(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null)
            return Message.Raw(OverrideLabel);

        if (Control == null) return null;
        var text = FindChildText(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "checkbox";

    public override Message? GetStatusString()
    {
        if (Control is not NCardPoolFilter filter)
            return null;

        var key = filter.IsSelected ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED";
        var text = LocalizationManager.Get("ui", key);
        return text != null ? Message.Raw(text) : null;
    }

    public override Message? GetTooltip()
    {
        if (Control is NCardPoolFilter filter && filter.Loc != null)
            return Message.Raw(filter.Loc.GetFormattedText());

        return null;
    }
}
