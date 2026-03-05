using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Sts2AccessibilityMod.UI;

public static class ProxyFactory
{
    public static ProxyElement Create(Control control)
    {
        // Most specific types first
        if (control is NTickbox)
            return new ProxyCheckbox(control);

        if (control is NDropdown)
            return new ProxyDropdown(control);

        if (control is NSettingsSlider)
            return new ProxySlider(control);

        if (control is NPaginator)
            return new ProxyPaginator(control);

        // Generic NButton and all other NClickableControl subclasses fall through to button
        if (control is NButton)
            return new ProxyButton(control);

        // Fallback for any other focusable control
        return new ProxyButton(control);
    }
}
