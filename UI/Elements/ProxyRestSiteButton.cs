using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyRestSiteButton : ProxyElement
{
    public ProxyRestSiteButton(Control control) : base(control) { }

    private NRestSiteButton? Button => Control as NRestSiteButton;

    public override Message? GetLabel()
    {
        var option = Button?.Option;
        if (option == null) return Message.Raw(CleanNodeName(Control.Name));

        return Message.Raw(option.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetExtrasString()
    {
        var option = Button?.Option;
        if (option == null) return null;

        var desc = option.Description.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? Message.Raw(StripBbcode(desc)) : null;
    }
}
