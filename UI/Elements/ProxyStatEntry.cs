using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyStatEntry : ProxyElement
{
    public ProxyStatEntry(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        return Message.Raw(OverrideLabel ?? CleanNodeName(Control!.Name));
    }

    public override Message? GetExtrasString()
    {
        var values = GetValues();
        return values.Count switch
        {
            0 => null,
            1 => Message.Raw(values[0]),
            _ => Message.Raw(string.Join(", ", values)),
        };
    }

    public IReadOnlyList<string> GetValues()
    {
        if (Control is not NStatEntry entry)
            return [];

        var values = new List<string>();
        AddValue(values, GetNodeText(entry.GetNodeOrNull("%TopLabel") ?? entry.GetNodeOrNull("TopLabel")));
        AddValue(values, GetNodeText(entry.GetNodeOrNull("%BottomLabel") ?? entry.GetNodeOrNull("BottomLabel")));
        return values;
    }

    private static string? GetNodeText(Node? node)
    {
        return node switch
        {
            RichTextLabel rtl => StripBbcode(rtl.Text).Trim(),
            Label label => label.Text.Trim(),
            null => null,
            _ => FindChildTextPublic(node)?.Trim(),
        };
    }

    private static void AddValue(List<string> values, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (text.Trim().Equals("Not Implemented", System.StringComparison.OrdinalIgnoreCase))
            return;

        values.Add(text.Trim());
    }
}
