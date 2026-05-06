using System;
using System.IO;
using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModMenuScreen : Screen
{
    private readonly PanelContainer _root;
    private readonly NavigableContainer _navContainer;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.MOD_MENU");

    public ModMenuScreen()
    {
        _root = new PanelContainer { Name = "ModMenu" };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
        };
        _root.AddThemeStyleboxOverride("panel", bg);

        var centerContainer = new CenterContainer();
        _root.AddChild(centerContainer);

        var contentPanel = new PanelContainer();
        var contentBg = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 1f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        contentPanel.AddThemeStyleboxOverride("panel", contentBg);
        contentPanel.CustomMinimumSize = new Vector2(500, 0);
        centerContainer.AddChild(contentPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 16);
        contentPanel.AddChild(outerVBox);

        var title = new Label
        {
            Text = $"Say the Spire 2 v{ModEntry.Version}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);
        outerVBox.AddChild(new HSeparator());

        var itemList = new VBoxContainer();
        itemList.AddThemeConstantOverride("separation", 8);
        outerVBox.AddChild(itemList);

        _navContainer = new NavigableContainer
        {
            ContainerLabel = Message.Localized("ui", "CONTAINERS.MOD_MENU"),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        // Settings
        var settingsBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.SETTINGS", "Settings"));
        settingsBtn.OnActivated = () =>
        {
            var screen = new ModSettingsScreen(ModSettings.Root);
            ScreenManager.PushScreen(screen);
        };
        _navContainer.Add(settingsBtn);
        AddControl(itemList, settingsBtn);

        // View Documentation
        var docsBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.VIEW_DOCS", "View Documentation"));
        docsBtn.OnActivated = () =>
        {
            OpenLocalDoc("SayTheSpire2Docs/index.html");
        };
        _navContainer.Add(docsBtn);
        AddControl(itemList, docsBtn);

        // View Change Log
        var changelogBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.VIEW_CHANGELOG", "View Change Log"));
        changelogBtn.OnActivated = () =>
        {
            OpenLocalDoc("SayTheSpire2Docs/changes.html");
        };
        _navContainer.Add(changelogBtn);
        AddControl(itemList, changelogBtn);

        // Visit Latest Release Page
        var releaseBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.VISIT_RELEASE", "Visit Latest Release Page"));
        releaseBtn.OnActivated = () =>
        {
            OS.ShellOpen("https://github.com/bradjrenshaw/say-the-spire2/releases/latest");
            SpeechManager.Output(Message.Localized("ui", "SPEECH.OPENING_RELEASE"));
        };
        _navContainer.Add(releaseBtn);
        AddControl(itemList, releaseBtn);

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
        ClaimAction("mega_top_panel");
        ClaimAction("mega_peek");
        ClaimAction("mega_view_draw_pile");
        ClaimAction("mega_view_discard_pile");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
        ClaimAction("mega_view_map");
        ClaimAction("mod_settings");
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_root);
        _navContainer.FocusFirst();
    }

    public override void OnFocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = true;
        _navContainer.FocusFirst();
    }

    public override void OnUnfocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = false;
    }

    public override void OnPop()
    {
        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.QueueFree();
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "ui_cancel" || action.Key == "mod_settings")
        {
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.CLOSED"));
            return true;
        }

        return _navContainer.HandleAction(action);
    }

    private void OpenLocalDoc(string relativePath)
    {
        try
        {
            var gameDir = Path.GetDirectoryName(OS.GetExecutablePath());
            var fullPath = Path.Combine(gameDir!, relativePath);
            if (File.Exists(fullPath))
            {
                OS.ShellOpen(fullPath);
                SpeechManager.Output(Message.Localized("ui", "SPEECH.OPENING_DOCS"));
            }
            else
            {
                SpeechManager.Output(Message.Localized("ui", "SPEECH.DOCS_NOT_FOUND"));
            }
        }
        catch (Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Failed to open docs: {e.Message}");
            SpeechManager.Output(Message.Localized("ui", "SPEECH.DOCS_FAILED"));
        }
    }

    private void AddControl(VBoxContainer list, ButtonElement element)
    {
        var control = (Control)element.Node;
        control.FocusMode = Control.FocusModeEnum.All;
        list.AddChild(control);

        control.FocusEntered += () =>
        {
            _navContainer.SetFocusTo(element);
        };

        ((BaseButton)control).Pressed += () => element.Activate();
    }
}
