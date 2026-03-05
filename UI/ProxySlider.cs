using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Sts2AccessibilityMod.UI;

public class ProxySlider : ProxyElement
{
    public ProxySlider(Control control) : base(control) { }

    public override string? GetLabel()
    {
        return OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "slider";

    public override string? GetStatusString()
    {
        if (Control is NSettingsSlider)
        {
            // The value label is a child named "SliderValue"
            var valueLabel = Control.GetNodeOrNull("SliderValue");
            if (valueLabel != null)
            {
                var text = FindChildText(valueLabel);
                if (text != null) return text;
            }
        }
        return null;
    }
}
