using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.UI.Elements;
using ListContainer = SayTheSpire2.UI.Elements.ListContainer;

namespace SayTheSpire2.UI.Screens;

public class SettingsGameScreen : GameScreen
{
    public static SettingsGameScreen? Current { get; private set; }

    private readonly NSettingsScreen _screen;

    public override string ScreenName => "Settings";

    // Tab node name -> panel node path, in order
    private static readonly (string tabName, string panelPath)[] TabPanels =
    {
        ("General", "%GeneralSettings"),
        ("Graphics", "%GraphicsSettings"),
        ("Sound", "%SoundSettings"),
        ("Input", "%InputSettings"),
    };

    public SettingsGameScreen(NSettingsScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    protected override void BuildRegistry()
    {
        var root = new ListContainer { AnnounceName = false, AnnouncePosition = false };
        var tabManager = _screen.GetNodeOrNull("SettingsTabManager");
        var positioners = new List<(NDropdownPositioner positioner, ListContainer container)>();

        foreach (var (tabName, panelPath) in TabPanels)
        {
            var panel = _screen.GetNodeOrNull<NSettingsPanel>(panelPath);
            if (panel == null) continue;

            // Read the localized tab label from the tab node
            var tabLabel = GetTabLabel(tabManager, tabName) ?? tabName;

            var tabContainer = new ListContainer
            {
                ContainerLabel = tabLabel,
                AnnounceName = true,
                AnnouncePosition = true,
            };

            RegisterControlsRecursive(panel, tabContainer, positioners);
            root.Add(tabContainer);
        }

        // Second pass: register positioner dropdowns last so their labels win
        foreach (var (positioner, container) in positioners)
        {
            RegisterDropdownPositioner(positioner, container);
        }

        RootElement = root;
        FocusContext?.Reset();
    }

    private void RegisterControlsRecursive(
        Node parent,
        ListContainer container,
        List<(NDropdownPositioner, ListContainer)> positioners)
    {
        foreach (var child in parent.GetChildren().OfType<Control>())
        {
            if (child is NDropdownPositioner positioner)
            {
                positioners.Add((positioner, container));
            }
            else if (IsSettingsOption(child))
            {
                if (child.FocusMode == Control.FocusModeEnum.All)
                    RegisterSettingsControl(child, container);
            }
            else
            {
                RegisterControlsRecursive(child, container, positioners);
            }
        }
    }

    private static bool IsSettingsOption(Control c)
    {
        if (c is NOpenModdingScreenButton) return false;
        return c is NTickbox or NPaginator or NSettingsSlider or NDropdownPositioner
            || (c is NButton btn && btn.IsEnabled);
    }

    private void RegisterSettingsControl(Control control, ListContainer container)
    {
        var label = FindLabelInParent(control);
        ProxyElement proxy;

        if (control is NInputSettingsEntry)
            proxy = new ProxyInputBinding(control);
        else if (control is NTickbox)
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
            proxy = new ProxyButton(control);
            Log.Info($"[AccessibilityMod] Unknown settings control: {control.GetType().Name} ({control.Name})");
            ConnectFocusSignal(control);
        }

        if (label != null) proxy.OverrideLabel = label;
        container.Add(proxy);
        Register(control, proxy);
    }

    private void ConnectFocusSignal(Control control)
    {
        control.FocusEntered += () =>
        {
            UIManager.QueueFocus(control, GetElement(control));
        };
    }

    private void RegisterDropdownPositioner(NDropdownPositioner positioner, ListContainer container)
    {
        var label = FindLabelInParent(positioner);

        var field = typeof(NDropdownPositioner).GetField("_dropdownNode", BindingFlags.Instance | BindingFlags.NonPublic);
        var dropdownNode = field?.GetValue(positioner) as Control;

        var proxy = new ProxyDropdown(dropdownNode ?? (Control)positioner);
        if (label != null) proxy.OverrideLabel = label;
        container.Add(proxy);
        Register(positioner, proxy);

        ConnectFocusSignal(positioner);
    }

    private static string? GetTabLabel(Node? tabManager, string tabNodeName)
    {
        if (tabManager == null) return null;
        var tab = tabManager.GetNodeOrNull(tabNodeName);
        if (tab == null) return null;
        return ProxyElement.FindChildTextPublic(tab);
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
