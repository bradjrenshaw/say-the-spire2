using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardBundle : ProxyElement
{
    public ProxyCardBundle(Control control) : base(control) { }

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

    public override string? GetLabel()
    {
        var bundle = FindBundle();
        if (bundle == null) return "Card Pack";

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
            var title = card.Title;
            if (!string.IsNullOrEmpty(title))
                cardNames.Add(title);
        }

        var cards = cardNames.Count > 0 ? string.Join(", ", cardNames) : "empty";

        if (total > 1)
            return $"Pack {index} of {total}: {cards}";
        return $"Pack: {cards}";
    }

    public override string? GetTypeKey() => "card";

    public override string? HandleBuffers(BufferManager buffers)
    {
        return base.HandleBuffers(buffers);
    }
}
