using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Sts2AccessibilityMod.Speech;

namespace Sts2AccessibilityMod.UI.Screens;

public class SettingsGameScreen : GameScreen
{
    private readonly NSettingsScreen _screen;

    public override string ScreenName => "Settings";

    public SettingsGameScreen(NSettingsScreen screen)
    {
        _screen = screen;
    }

    protected override void BuildRegistry()
    {
        var panelNames = new[] { "%GeneralSettings", "%GraphicsSettings", "%SoundSettings", "%InputSettings" };
        var positioners = new System.Collections.Generic.List<NDropdownPositioner>();

        // First pass: register all controls, collecting positioners for later
        foreach (var panelName in panelNames)
        {
            var panel = _screen.GetNodeOrNull<NSettingsPanel>(panelName);
            if (panel == null) continue;

            RegisterControlsRecursive(panel, positioners);
        }

        // Second pass: register positioner dropdowns last so their labels win
        foreach (var positioner in positioners)
        {
            RegisterDropdownPositioner(positioner);
        }
    }

    private void RegisterControlsRecursive(Node parent, System.Collections.Generic.List<NDropdownPositioner> positioners)
    {
        foreach (var child in parent.GetChildren().OfType<Control>())
        {
            if (child is NDropdownPositioner positioner)
            {
                positioners.Add(positioner);
            }
            else if (child.FocusMode != Control.FocusModeEnum.None)
            {
                RegisterSettingsControl(child);
            }
            else
            {
                RegisterControlsRecursive(child, positioners);
            }
        }
    }

    private void RegisterSettingsControl(Control control)
    {
        var label = FindLabelInParent(control);
        ProxyElement proxy;

        if (control is NTickbox)
            proxy = new ProxyCheckbox(control);
        else if (control is NDropdown)
            proxy = new ProxyDropdown(control);
        else if (control is NSettingsSlider)
            proxy = new ProxySlider(control);
        else if (control is NPaginator)
            proxy = new ProxyPaginator(control);
        else if (control is NButton)
            proxy = new ProxyButton(control);
        else
        {
            // Unknown focusable control — register as button fallback and
            // connect to FocusEntered signal since we have no hook for it
            proxy = new ProxyButton(control);
            Log.Info($"[AccessibilityMod] Unknown settings control: {control.GetType().Name} ({control.Name})");
            ConnectFocusSignal(control);
        }

        if (label != null) proxy.OverrideLabel = label;
        Register(control, proxy);
    }

    private void ConnectFocusSignal(Control control)
    {
        control.FocusEntered += () =>
        {
            var element = GetElement(control);
            if (element == null) return;
            var text = element.GetFocusString();
            Log.Info($"[AccessibilityMod] Focus (signal): {control.GetType().Name} ({control.Name}) -> \"{text}\"");
            if (!string.IsNullOrEmpty(text))
            {
                SpeechManager.Output(text);
            }
        };
    }

    private void RegisterDropdownPositioner(NDropdownPositioner positioner)
    {
        var label = FindLabelInParent(positioner);

        // Extract the child dropdown to read its value from
        var field = typeof(NDropdownPositioner).GetField("_dropdownNode", BindingFlags.Instance | BindingFlags.NonPublic);
        var dropdownNode = field?.GetValue(positioner) as Control;

        // Register the positioner itself — that's what gets focus
        var proxy = new ProxyDropdown(dropdownNode ?? (Control)positioner);
        if (label != null) proxy.OverrideLabel = label;
        Register(positioner, proxy);

        // Connect FocusEntered since positioner isn't an NClickableControl
        ConnectFocusSignal(positioner);
    }

    private static string? FindLabelInParent(Control control)
    {
        var parent = control.GetParent();
        if (parent == null) return null;

        var labelNode = parent.GetNodeOrNull("Label");
        if (labelNode is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return ProxyElement.StripBbcode(rtl.Text);
        if (labelNode is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;

        return null;
    }
}
