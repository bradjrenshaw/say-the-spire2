using System.Text.RegularExpressions;
using Godot;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public abstract class ProxyElement : UIElement
{
    private static readonly Regex CamelCasePattern = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected Control Control { get; private set; }

    public string? OverrideLabel { get; set; }

    public override bool IsVisible =>
        GodotObject.IsInstanceValid(Control) && Control.IsVisibleInTree();

    protected ProxyElement(Control control)
    {
        Control = control;
    }

    public void SetControl(Control control)
    {
        Control = control;
    }

    public static string? FindChildTextPublic(Node node) => FindChildText(node);

    protected static string? FindChildText(Node node)
    {
        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;
        if (node is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return StripBbcode(rtl.Text);

        // Check well-known child names first
        foreach (var childName in new[] { "Title", "Label", "%Label", "%Title" })
        {
            var child = node.GetNodeOrNull(childName);
            if (child != null)
            {
                var text = FindChildText(child);
                if (text != null) return text;
            }
        }

        // Walk all children
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var child = node.GetChild(i);
            var text = FindChildText(child);
            if (text != null) return text;
        }

        return null;
    }

    protected static string? FindSiblingLabel(Node node)
    {
        var parent = node.GetParent();
        if (parent == null) return null;

        // Look for a Label sibling in the parent container
        foreach (var childName in new[] { "Label", "%Label", "Title", "%Title" })
        {
            var sibling = parent.GetNodeOrNull(childName);
            if (sibling != null && sibling != node)
            {
                var text = FindChildText(sibling);
                if (text != null) return text;
            }
        }

        return null;
    }

    public static string StripBbcode(string text) => Message.StripBbcode(text);

    protected static string CleanNodeName(string name)
    {
        return CamelCasePattern.Replace(name, "$1 $2");
    }
}
