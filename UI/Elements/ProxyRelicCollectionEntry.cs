using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyRelicCollectionEntry : ProxyElement
{
    public ProxyRelicCollectionEntry(Control control) : base(control) { }

    private NRelicCollectionEntry? Entry => Control as NRelicCollectionEntry;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("relic");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var entry = Entry;
        if (entry == null)
            return null;

        return entry.ModelVisibility == ModelVisibility.Visible
            ? Message.Raw(entry.relic.Title.GetFormattedText())
            : Message.Localized("ui", "RELIC.UNKNOWN");
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
