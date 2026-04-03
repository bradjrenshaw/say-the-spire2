using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CustomRunLoadGameScreen : GameScreen
{
    private static readonly System.Reflection.FieldInfo? LobbyField =
        AccessTools.Field(typeof(NCustomRunLoadScreen), "_lobby");

    public static CustomRunLoadGameScreen? Current { get; private set; }

    private readonly NCustomRunLoadScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = new LocString("main_menu_ui", "CUSTOM_RUN_SCREEN.CUSTOM_MODE_TITLE").GetFormattedText(),
        AnnounceName = true,
        AnnouncePosition = false,
    };
    private readonly HashSet<ulong> _connectedControls = new();
    private readonly Dictionary<ulong, UIElement> _elementCache = new();

    private string? _stateToken;
    private int _lastAscension = -1;

    private LineEdit? _seedInput;
    private NAscensionPanel? _ascensionPanel;
    private NClickableControl? _ascensionLeftArrow;
    private NClickableControl? _ascensionRightArrow;
    private NCustomRunModifiersList? _modifiersList;
    private List<NRunModifierTickbox> _modifierTickboxes = new();
    private NClickableControl? _confirmButton;
    private NClickableControl? _unreadyButton;
    private NClickableControl? _backButton;
    private readonly ListContainer _seedRow = NewRow(UiStatic("CUSTOM_RUN.ROWS.SEED"), announcePosition: false);
    private readonly ListContainer _ascensionRow = NewRow(UiStatic("CUSTOM_RUN.ROWS.ASCENSION"), announcePosition: false);
    private readonly ListContainer _modifierRow = NewRow(UiStatic("CUSTOM_RUN.ROWS.MODIFIERS"));

    public override string? ScreenName => new LocString("main_menu_ui", "CUSTOM_RUN_SCREEN.CUSTOM_MODE_TITLE").GetFormattedText();

    public CustomRunLoadGameScreen(NCustomRunLoadScreen screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
    }

    public override void OnPush()
    {
        Current = this;
        ResolveControls();
        _stateToken = BuildStateToken();
        base.OnPush();
        EnsureFocus();
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this)
            Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen))
            return;

        ResolveControls();
        PollAscension();

        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }

        EnsureFocus();
    }

    public void OnStateChanged()
    {
        _stateToken = null;
    }

    public void OnPlayerConnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_JOINED", new { player = GetPlayerName(playerId) }));
        _stateToken = null;
    }

    public void OnPlayerReadyChanged(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        var lobby = Lobby;
        if (lobby != null && playerId == lobby.NetService.NetId)
        {
            _stateToken = null;
            return;
        }

        var status = lobby?.IsPlayerReady(playerId) == true ? Ui("DAILY_RUN.READY") : Ui("DAILY_RUN.NOT_READY");
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOAD_LOBBY_CHANGED", new
        {
            player = GetPlayerName(playerId),
            status,
        }));
        _stateToken = null;
    }

    public void OnPlayerDisconnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_LEFT", new { player = GetPlayerName(playerId) }));
        _stateToken = null;
    }

    public void OnLocalDisconnected()
    {
        if (IsMultiplayer())
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_DISCONNECTED"));
    }

    public void OnEmbarkPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.MARKED_READY"));
        _stateToken = null;
    }

    public void OnUnreadyPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.NO_LONGER_READY"));
        _stateToken = null;
    }

    public override bool OnActionJustPressed(Input.InputAction action)
    {
        switch (action.Key)
        {
            case "ui_cancel":
            case "mega_pause_and_back":
                Activate(_backButton);
                return true;
            default:
                return false;
        }
    }

    protected override void BuildRegistry()
    {
        _root.Clear();
        _seedRow.Clear();
        _ascensionRow.Clear();
        _modifierRow.Clear();

        if (_seedInput != null)
            RegisterRowItem(_seedRow, _seedInput, GetOrCreate(_seedInput, () => ProxyFactory.Create(_seedInput)));

        if (_ascensionPanel != null && IsUsable(_ascensionPanel))
        {
            var ascensionElement = GetOrCreate(_ascensionPanel, () => new ActionElement(
                () => Ui("CUSTOM_RUN.ASCENSION", new { value = _ascensionPanel?.Ascension ?? 0 }),
                status: GetAscensionStatus));
            RegisterRowItem(_ascensionRow, _ascensionPanel, ascensionElement);
            RegisterAlias(_ascensionLeftArrow, ascensionElement);
            RegisterAlias(_ascensionRightArrow, ascensionElement);
        }

        foreach (var tickbox in _modifierTickboxes.Where(IsUsable))
            RegisterRowItem(_modifierRow, tickbox, GetOrCreate(tickbox, () => ProxyFactory.Create(tickbox)));

        AddRowIfNotEmpty(_seedRow);
        AddRowIfNotEmpty(_ascensionRow);
        AddRowIfNotEmpty(_modifierRow);

        if (_confirmButton != null && IsUsable(_confirmButton))
            RegisterMain(_confirmButton, GetOrCreate(_confirmButton, CreateConfirmButtonElement));
        if (_unreadyButton != null && IsUsable(_unreadyButton))
            RegisterMain(_unreadyButton, GetOrCreate(_unreadyButton, CreateUnreadyButtonElement));
        if (_backButton != null && IsUsable(_backButton))
            RegisterMain(_backButton, GetOrCreate(_backButton, CreateBackButtonElement));

        WireFocusNeighbors();
    }

    private void ResolveControls()
    {
        _seedInput ??= _screen.GetNodeOrNull<LineEdit>("%SeedInput");
        _ascensionPanel ??= _screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        _ascensionLeftArrow ??= _ascensionPanel?.GetNodeOrNull<NClickableControl>("HBoxContainer/LeftArrowContainer/LeftArrow");
        _ascensionRightArrow ??= _ascensionPanel?.GetNodeOrNull<NClickableControl>("HBoxContainer/RightArrowContainer/RightArrow");
        _modifiersList ??= _screen.GetNodeOrNull<NCustomRunModifiersList>("%ModifiersList");
        if (_modifiersList != null)
        {
            var content = _modifiersList.GetNodeOrNull<Control>("ScrollContainer/Mask/Content");
            if (content != null)
                _modifierTickboxes = content.GetChildren().OfType<NRunModifierTickbox>().ToList();
        }
        _confirmButton ??= _screen.GetNodeOrNull<NClickableControl>("ConfirmButton");
        _unreadyButton ??= _screen.GetNodeOrNull<NClickableControl>("UnreadyButton");
        _backButton ??= _screen.GetNodeOrNull<NClickableControl>("BackButton");
    }

    private void PollAscension()
    {
        if (_ascensionPanel == null || !_ascensionPanel.Visible)
            return;

        var current = _ascensionPanel.Ascension;
        if (_lastAscension == -1)
        {
            _lastAscension = current;
            return;
        }

        if (current == _lastAscension)
            return;

        _lastAscension = current;
        var title = AscensionHelper.GetTitle(current).GetFormattedText();
        var description = AscensionHelper.GetDescription(current).GetFormattedText();
        SpeechManager.Output(Message.Raw($"{Ui("CUSTOM_RUN.ASCENSION", new { value = current })}: {title}. {description}"));
        _stateToken = null;
    }

    private void RegisterMain(Control control, UIElement element)
    {
        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
        _root.Add(element);
    }

    private void RegisterRowItem(ListContainer row, Control control, UIElement element)
    {
        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
        row.Add(element);
    }

    private void RegisterAlias(Control? control, UIElement element)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    private void WireFocusNeighbors()
    {
        var seed = IsUsable(_seedInput) ? _seedInput : null;
        var ascension = IsUsable(_ascensionPanel) ? _ascensionPanel : null;
        var modifiers = _modifierTickboxes.Where(IsUsable).Cast<Control>().ToList();
        var buttons = new[] { _confirmButton, _unreadyButton, _backButton }.Where(IsUsable).Cast<Control>().ToList();

        var firstModifier = modifiers.FirstOrDefault();
        var firstButton = buttons.FirstOrDefault();

        if (seed != null)
        {
            var self = seed.GetPath();
            Control rightTarget = (Control?)ascension ?? firstModifier ?? firstButton ?? seed;
            seed.FocusNeighborTop = self;
            seed.FocusNeighborLeft = self;
            seed.FocusNeighborRight = rightTarget.GetPath();
            seed.FocusNeighborBottom = rightTarget.GetPath();
        }

        if (ascension != null)
        {
            var targetAbove = ((Control?)seed ?? ascension).GetPath();
            var targetBelow = (firstModifier ?? firstButton ?? ascension).GetPath();
            var self = ascension.GetPath();
            ascension.FocusNeighborTop = targetAbove;
            ascension.FocusNeighborBottom = targetBelow;
            ascension.FocusNeighborLeft = self;
            ascension.FocusNeighborRight = self;

            if (IsUsable(_ascensionLeftArrow))
            {
                _ascensionLeftArrow!.FocusNeighborTop = targetAbove;
                _ascensionLeftArrow.FocusNeighborBottom = targetBelow;
                _ascensionLeftArrow.FocusNeighborLeft = _ascensionLeftArrow.GetPath();
                _ascensionLeftArrow.FocusNeighborRight = IsUsable(_ascensionRightArrow)
                    ? _ascensionRightArrow!.GetPath()
                    : self;
            }

            if (IsUsable(_ascensionRightArrow))
            {
                _ascensionRightArrow!.FocusNeighborTop = targetAbove;
                _ascensionRightArrow.FocusNeighborBottom = targetBelow;
                _ascensionRightArrow.FocusNeighborLeft = IsUsable(_ascensionLeftArrow)
                    ? _ascensionLeftArrow!.GetPath()
                    : self;
                _ascensionRightArrow.FocusNeighborRight = _ascensionRightArrow.GetPath();
            }
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            var self = modifiers[i].GetPath();
            modifiers[i].FocusNeighborLeft = i > 0 ? modifiers[i - 1].GetPath() : self;
            modifiers[i].FocusNeighborRight = i < modifiers.Count - 1 ? modifiers[i + 1].GetPath() : self;
            modifiers[i].FocusNeighborTop = (ascension ?? seed ?? modifiers[i]).GetPath();
            modifiers[i].FocusNeighborBottom = (firstButton ?? modifiers[i]).GetPath();
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var self = buttons[i].GetPath();
            buttons[i].FocusNeighborLeft = i > 0 ? buttons[i - 1].GetPath() : self;
            buttons[i].FocusNeighborRight = i < buttons.Count - 1 ? buttons[i + 1].GetPath() : self;
            buttons[i].FocusNeighborTop = (firstModifier ?? ascension ?? seed ?? buttons[i]).GetPath();
            buttons[i].FocusNeighborBottom = self;
        }
    }

    private void EnsureFocus()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focusOwner != null && GetElement(focusOwner) != null)
            return;

        GetInitialFocusTarget()?.GrabFocus();
    }

    private Control? GetInitialFocusTarget()
    {
        if (IsUsable(_seedInput))
            return _seedInput;
        if (IsUsable(_ascensionPanel))
            return _ascensionPanel;
        if (_modifierTickboxes.FirstOrDefault(IsUsable) is { } modifier)
            return modifier;
        if (IsUsable(_confirmButton))
            return _confirmButton;
        if (IsUsable(_unreadyButton))
            return _unreadyButton;
        if (IsUsable(_backButton))
            return _backButton;
        return null;
    }

    private string BuildStateToken()
    {
        var parts = new List<string>
        {
            $"{_seedInput?.Text}|{_seedInput?.Editable}|{_seedInput?.Visible}",
            $"{_ascensionPanel?.Visible}|{_ascensionPanel?.Ascension}"
        };

        parts.AddRange(_modifierTickboxes.Select(t => $"{t.GetInstanceId()}:{t.Visible}:{t.IsEnabled}"));
        parts.Add($"{_confirmButton?.Visible}:{_confirmButton?.IsEnabled}");
        parts.Add($"{_unreadyButton?.Visible}:{_unreadyButton?.IsEnabled}");
        parts.Add($"{_backButton?.Visible}:{_backButton?.IsEnabled}");
        return string.Join("|", parts);
    }

    private static ListContainer NewRow(string label, bool announcePosition = true) => new()
    {
        ContainerLabel = label,
        AnnounceName = true,
        AnnouncePosition = announcePosition,
    };

    private void AddRowIfNotEmpty(ListContainer row)
    {
        if (row.Children.Count > 0)
            _root.Add(row);
    }

    private string? GetAscensionStatus()
    {
        if (_ascensionPanel == null)
            return null;

        var value = _ascensionPanel.Ascension;
        var title = AscensionHelper.GetTitle(value).GetFormattedText();
        var description = AscensionHelper.GetDescription(value).GetFormattedText();
        return string.IsNullOrWhiteSpace(description) ? title : $"{title}. {description}";
    }

    private UIElement GetOrCreate(Control control, Func<UIElement> factory)
    {
        if (_elementCache.TryGetValue(control.GetInstanceId(), out var existing))
            return existing;

        var created = factory();
        _elementCache[control.GetInstanceId()] = created;
        return created;
    }

    private static bool IsUsable(Control? control)
    {
        return control != null
            && GodotObject.IsInstanceValid(control)
            && control.Visible;
    }

    private LoadRunLobby? Lobby => LobbyField?.GetValue(_screen) as LoadRunLobby;

    private bool IsMultiplayer()
    {
        return Lobby?.NetService.Type is NetGameType.Host or NetGameType.Client;
    }

    private string GetPlayerName(ulong playerId)
    {
        return MultiplayerHelper.GetPlayerName(playerId, Lobby?.NetService.Platform);
    }

    private string Ui(string key, object? data = null)
    {
        return data == null
            ? LocalizationManager.GetOrDefault("ui", key, key)
            : Message.Localized("ui", key, data).Resolve();
    }

    private static void Activate(NClickableControl? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.EmitSignal(NClickableControl.SignalName.Released, control);
    }

    private static string UiStatic(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }

    private UIElement CreateConfirmButtonElement()
    {
        return new ActionElement(
            () => Ui("DAILY_RUN.READY"),
            status: () => GetButtonStatus(_confirmButton),
            typeKey: () => "button");
    }

    private UIElement CreateUnreadyButtonElement()
    {
        return new ActionElement(
            () => Ui("DAILY_RUN.CANCEL_READY"),
            status: () => GetButtonStatus(_unreadyButton),
            typeKey: () => "button");
    }

    private UIElement CreateBackButtonElement()
    {
        return new ActionElement(
            () => Ui("DAILY_RUN.BACK"),
            status: () => GetButtonStatus(_backButton),
            typeKey: () => "button");
    }

    private static string? GetButtonStatus(NClickableControl? control)
    {
        if (control == null || control.IsEnabled)
            return null;

        return LocalizationManager.GetOrDefault("ui", "DAILY_RUN.DISABLED", "Disabled");
    }
}
