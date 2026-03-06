using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxySlider : ProxyElement
{
    private static readonly FieldInfo? SliderField =
        typeof(NSettingsSlider).GetField("_slider", BindingFlags.Instance | BindingFlags.NonPublic);

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
            var valueLabel = Control.GetNodeOrNull("SliderValue");
            if (valueLabel != null)
            {
                var text = FindChildText(valueLabel);
                if (text != null) return text;
            }
        }
        return null;
    }

    protected override void OnFocus()
    {
        var slider = GetInnerSlider();
        if (slider != null)
            slider.ValueChanged += OnValueChanged;
    }

    protected override void OnUnfocus()
    {
        var slider = GetInnerSlider();
        if (slider != null)
            slider.ValueChanged -= OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        var status = GetStatusString();
        if (!string.IsNullOrEmpty(status))
            SpeechManager.Output(status);
    }

    private Range? GetInnerSlider()
    {
        if (Control is NSettingsSlider)
            return SliderField?.GetValue(Control) as Range;
        return null;
    }
}
