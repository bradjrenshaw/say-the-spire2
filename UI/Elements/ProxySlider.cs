using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxySlider : ProxyElement
{
    private static readonly FieldInfo? SliderField =
        AccessTools.Field(typeof(NSettingsSlider), "_slider");

    public ProxySlider(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        var text = OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "slider";

    public override Message? GetStatusString()
    {
        if (Control is NSettingsSlider)
        {
            var valueLabel = Control.GetNodeOrNull("SliderValue");
            if (valueLabel != null)
            {
                var text = FindChildText(valueLabel);
                if (text != null) return Message.Raw(text);
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
        if (status != null)
            SpeechManager.Output(status);
    }

    private Range? GetInnerSlider()
    {
        if (Control is NSettingsSlider)
            return SliderField?.GetValue(Control) as Range;
        return null;
    }
}
