using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

// User-perceives this as a button; share settings / [AnnouncementOrder] with ProxyButton.
public class ProxyCardBundle : ProxyElement
{
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyCardBundle(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
        yield return new TypeAnnouncement("card");
    }

    private NCardBundle? FindBundle()
    {
        Node? current = Control?.GetParent();
        while (current != null)
        {
            if (current is NCardBundle bundle)
                return bundle;
            current = current.GetParent();
        }
        return null;
    }

    public override Message? GetLabel()
    {
        var bundle = FindBundle();
        if (bundle == null) return Message.Localized("ui", "LABELS.CARD_PACK");

        // Determine position among siblings
        var parent = bundle.GetParent();
        int index = 0;
        int total = 0;
        if (parent != null)
        {
            for (int i = 0; i < parent.GetChildCount(); i++)
            {
                if (parent.GetChild(i) is NCardBundle)
                {
                    total++;
                    if (parent.GetChild(i) == bundle)
                        index = total;
                }
            }
        }

        var cardNames = new List<string>();
        foreach (var card in bundle.Bundle)
        {
            var view = CardView.FromModel(card);
            if (!string.IsNullOrEmpty(view.Title))
                cardNames.Add(view.Title);
        }

        var cards = cardNames.Count > 0 ? string.Join(", ", cardNames) : LocalizationManager.GetOrDefault("ui", "LABELS.EMPTY", "empty");

        if (total > 1)
            return Message.Localized("ui", "CARD.PACK_POSITION", new { index, total, cards });
        return Message.Localized("ui", "CARD.PACK", new { cards });
    }

    public override string? GetTypeKey() => "card";

    public override string? HandleBuffers(BufferManager buffers)
    {
        return base.HandleBuffers(buffers);
    }
}
