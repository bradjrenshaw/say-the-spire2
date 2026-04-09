using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyRelicCollectionEntry : ProxyElement
{
    public ProxyRelicCollectionEntry(Control control) : base(control) { }

    private NRelicCollectionEntry? Entry => Control as NRelicCollectionEntry;

    public override Message? GetLabel()
    {
        var entry = Entry;
        if (entry == null)
            return null;

        return entry.ModelVisibility == ModelVisibility.Visible
            ? Message.Raw(entry.relic.Title.GetFormattedText())
            : Message.Raw("Unknown relic");
    }

    public override string? GetTypeKey() => "relic";

    public override Message? GetStatusString()
    {
        var text = Entry?.ModelVisibility switch
        {
            ModelVisibility.Locked => "Locked",
            ModelVisibility.NotSeen => "Undiscovered",
            _ => (string?)null,
        };
        return text != null ? Message.Raw(text) : null;
    }

    public override Message? GetTooltip()
    {
        var entry = Entry;
        if (entry == null)
            return null;

        var text = entry.ModelVisibility switch
        {
            ModelVisibility.Visible => StripBbcode(entry.relic.DynamicDescription.GetFormattedText()),
            ModelVisibility.NotSeen => new LocString("main_menu_ui", "COMPENDIUM_RELIC_COLLECTION.unknown.description").GetFormattedText(),
            ModelVisibility.Locked => new LocString("main_menu_ui", "COMPENDIUM_RELIC_COLLECTION.locked.description").GetFormattedText(),
            _ => (string?)null,
        };
        return text != null ? Message.Raw(text) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entry = Entry;
        if (entry == null || entry.ModelVisibility != ModelVisibility.Visible)
            return base.HandleBuffers(buffers);

        return ProxyRelicHolder.FromModel(entry.relic).HandleBuffers(buffers);
    }
}
