using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Sts2AccessibilityMod.UI;

public static class ProxyFactory
{
    public static ProxyElement Create(Control control)
    {
        // Most specific types first
        if (control is NCharacterSelectButton)
            return new ProxyCharacterButton(control);

        if (control is NInputSettingsEntry)
            return new ProxyInputBinding(control);

        if (control is NTickbox)
            return new ProxyCheckbox(control);

        if (control is NDropdown)
            return new ProxyDropdown(control);

        if (control is NSettingsSlider)
            return new ProxySlider(control);

        if (control is NPaginator)
            return new ProxyPaginator(control);

        // Check if this control is a hitbox inside a card holder or creature
        var ancestor = FindAncestor(control);
        if (ancestor != null) return ancestor;

        // Generic NButton and all other NClickableControl subclasses fall through to button
        if (control is NButton)
            return new ProxyButton(control);

        // Fallback for any other focusable control
        return new ProxyButton(control);
    }

    private static ProxyElement? FindAncestor(Control control)
    {
        Node? current = control.GetParent();
        while (current != null)
        {
            if (current is NCardHolder)
                return new ProxyCard(control);
            if (current is NCreature)
                return new ProxyCreature(control);
            current = current.GetParent();
        }
        return null;
    }
}
