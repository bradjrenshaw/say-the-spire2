using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Saves.Runs;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class DailyRunLoadGameScreen : GameScreen
{
    private static readonly FieldInfo? LobbyField =
        AccessTools.Field(typeof(NDailyRunLoadScreen), "_lobby");

    public static DailyRunLoadGameScreen? Current { get; private set; }

    private readonly NDailyRunLoadScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Ui("DAILY_RUN.SCREEN_NAME"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly HashSet<ulong> _connectedControls = new();
    private string? _stateToken;

    private NDailyRunCharacterContainer? _characterContainer;
    private Label? _dateLabel;
    private List<NDailyRunScreenModifier> _modifierControls = new();
    private NDailyRunLeaderboard? _leaderboard;
    private Control? _scoreWarning;
    private NClickableControl? _embarkButton;
    private NClickableControl? _unreadyButton;
    private NClickableControl? _backButton;

    public override string? ScreenName => Ui("DAILY_RUN.SCREEN_NAME");

    public DailyRunLoadGameScreen(NDailyRunLoadScreen screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
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
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        ResolveControls();
        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }

        EnsureFocus();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_cancel":
            case "mega_pause_and_back":
                if (TryCloseLeaderboardChild())
                    return true;

                Activate(_backButton);
                return true;
            case "ui_accept":
            case "ui_select":
                return ActivateFocusedControl();
            default:
                return false;
        }
    }

    public void OnPlayerConnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_JOINED", new { player = GetPlayerName(playerId) }));
    }

    public void OnPlayerReadyChanged(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        var lobby = Lobby;
        if (lobby != null && playerId == lobby.NetService.NetId)
            return;

        var status = Lobby?.IsPlayerReady(playerId) == true ? Ui("DAILY_RUN.READY") : Ui("DAILY_RUN.NOT_READY");
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOAD_LOBBY_CHANGED", new
        {
            player = GetPlayerName(playerId),
            status,
        }));
    }

    public void OnPlayerDisconnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_LEFT", new { player = GetPlayerName(playerId) }));
    }

    public void OnLocalDisconnected()
    {
        if (IsMultiplayer())
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_DISCONNECTED"));
    }

    public void OnEmbarkPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.MARKED_READY"));
    }

    public void OnUnreadyPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.NO_LONGER_READY"));
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterFocusable(_characterContainer, new ActionElement(
            () => GetCharacterLabel(),
            status: () => GetCharacterStatus()));

        RegisterFocusable(_dateLabel, new ActionElement(
            () => GetDateText() ?? Ui("DAILY_RUN.SAVE_FALLBACK")));

        for (int i = 0; i < _modifierControls.Count; i++)
        {
            int index = i;
            RegisterFocusable(_modifierControls[i], new ActionElement(
                () => GetModifierText(index) ?? Ui("DAILY_RUN.MODIFIER", new { index = index + 1 }),
                isVisible: () => GetModifierVisible(index)));
        }

        RegisterFocusable(_leaderboard, new ActionElement(
            () => Ui("DAILY_RUN.LEADERBOARD"),
            typeKey: () => "button",
            status: () => GetLeaderboardStatus(),
            isVisible: () => _leaderboard?.Visible == true));

        RegisterFocusable(_scoreWarning, new ActionElement(
            () => new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.title").GetFormattedText(),
            tooltip: () => new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.description").GetFormattedText(),
            isVisible: () => _scoreWarning?.Visible == true));

        RegisterFocusable(_embarkButton, new ActionElement(
            () => Ui("DAILY_RUN.READY"),
            status: () => GetButtonStatus(_embarkButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_embarkButton)));

        RegisterFocusable(_unreadyButton, new ActionElement(
            () => Ui("DAILY_RUN.CANCEL_READY"),
            status: () => GetButtonStatus(_unreadyButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_unreadyButton)));

        RegisterFocusable(_backButton, new ActionElement(
            () => Ui("DAILY_RUN.BACK"),
            status: () => GetButtonStatus(_backButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_backButton)));

        WireFocusNeighbors();
    }

    private void ResolveControls()
    {
        if (!GodotObject.IsInstanceValid(_screen))
            return;

        _dateLabel ??= _screen.GetNodeOrNull<Label>("%Date");
        _characterContainer ??= _screen.GetNodeOrNull<NDailyRunCharacterContainer>("%CharacterContainer");
        if (_modifierControls.Count == 0)
        {
            var container = _screen.GetNodeOrNull<Control>("%ModifiersContainer");
            if (container != null)
                _modifierControls = container.GetChildren().OfType<NDailyRunScreenModifier>().ToList();
        }
        _leaderboard ??= _screen.GetNodeOrNull<NDailyRunLeaderboard>("%Leaderboards");
        _scoreWarning ??= _leaderboard?.GetNodeOrNull<Control>("%ScoreWarning");
        _embarkButton ??= _screen.GetNodeOrNull<NClickableControl>("%ConfirmButton");
        _unreadyButton ??= _screen.GetNodeOrNull<NClickableControl>("%UnreadyButton");
        _backButton ??= _screen.GetNodeOrNull<NClickableControl>("%BackButton");
    }

    private void RegisterFocusable(Control? control, UIElement element)
    {
        if (control == null)
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
        _root.Add(element);
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    private void WireFocusNeighbors()
    {
        var controls = GetFocusableControls();
        for (int i = 0; i < controls.Count; i++)
        {
            var self = controls[i].GetPath();
            controls[i].FocusNeighborTop = i > 0 ? controls[i - 1].GetPath() : self;
            controls[i].FocusNeighborBottom = i < controls.Count - 1 ? controls[i + 1].GetPath() : self;
            controls[i].FocusNeighborLeft = self;
            controls[i].FocusNeighborRight = self;
        }
    }

    private List<Control> GetFocusableControls()
    {
        var controls = new List<Control>();
        AddIfVisible(controls, _characterContainer);
        AddIfVisible(controls, _dateLabel);
        foreach (var modifier in _modifierControls)
            AddIfVisible(controls, modifier);
        AddIfVisible(controls, _leaderboard);
        AddIfVisible(controls, _scoreWarning);
        AddIfVisible(controls, _embarkButton);
        AddIfVisible(controls, _unreadyButton);
        AddIfVisible(controls, _backButton);
        return controls;
    }

    private void EnsureFocus()
    {
        if (ActiveChild != null)
            return;

        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        var controls = GetFocusableControls();
        if (focusOwner != null && controls.Contains(focusOwner))
            return;

        controls.FirstOrDefault()?.GrabFocus();
    }

    private bool ActivateFocusedControl()
    {
        var focused = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focused == null || !_screen.IsAncestorOf(focused))
            return true;

        if (focused == _leaderboard)
        {
            OpenLeaderboard();
            return true;
        }

        if (focused is NClickableControl button)
        {
            Activate(button);
            return true;
        }

        return true;
    }

    private bool TryCloseLeaderboardChild()
    {
        if (ActiveChild is not DailyLeaderboardScreen child)
            return false;

        RemoveChild(child);
        if (_leaderboard != null)
        {
            var tree = _leaderboard.GetTree();
            if (tree != null)
                tree.CreateTimer(0).Timeout += RestoreLeaderboardFocus;
        }
        return true;
    }

    private void RestoreLeaderboardFocus()
    {
        if (_leaderboard == null)
            return;

        _leaderboard.GrabFocus();
        UIManager.SetFocusedControl(_leaderboard, GetElement(_leaderboard));
    }

    private LoadRunLobby? Lobby => LobbyField?.GetValue(_screen) as LoadRunLobby;

    private bool IsMultiplayer()
    {
        return Lobby?.NetService.Type is NetGameType.Host or NetGameType.Client;
    }

    private string GetCharacterLabel()
    {
        var player = GetLocalSerializablePlayer();
        return player?.CharacterId.ToString() ?? Ui("DAILY_RUN.CHARACTER");
    }

    private string? GetCharacterStatus()
    {
        var lobby = Lobby;
        var player = GetLocalSerializablePlayer();
        if (lobby == null || player == null)
            return null;

        var parts = new List<string>();
        if (IsMultiplayer())
            parts.Add(GetPlayerName(player.NetId));
        parts.Add(Ui("DAILY_RUN.ASCENSION", new { value = lobby.Run.Ascension }));
        if (lobby.IsPlayerReady(player.NetId))
            parts.Add(Ui("DAILY_RUN.READY"));
        return string.Join(", ", parts);
    }

    private SerializablePlayer? GetLocalSerializablePlayer()
    {
        var lobby = Lobby;
        return lobby?.Run.Players.FirstOrDefault(p => p.NetId == lobby.NetService.NetId);
    }

    private string? GetDateText()
    {
        return _dateLabel?.Text;
    }

    private string? GetModifierText(int index)
    {
        if (index < 0 || index >= _modifierControls.Count)
            return null;
        var description = _modifierControls[index].GetNodeOrNull<RichTextLabel>("Description");
        return description == null ? null : ProxyElement.StripBbcode(description.Text);
    }

    private bool GetModifierVisible(int index)
    {
        return index >= 0 && index < _modifierControls.Count && _modifierControls[index].Visible;
    }

    private string? GetLeaderboardStatus()
    {
        if (_leaderboard == null)
            return null;

        var dayLabel = _leaderboard.GetNodeOrNull<Label>("%DateLabel")?.Text;
        return string.IsNullOrWhiteSpace(dayLabel) ? null : dayLabel.Trim();
    }

    private void OpenLeaderboard()
    {
        if (_leaderboard != null)
            PushChild(new DailyLeaderboardScreen(_leaderboard, _leaderboard));
    }

    private string GetPlayerName(ulong playerId)
    {
        return MultiplayerHelper.GetPlayerName(playerId, Lobby?.NetService.Platform);
    }

    private string BuildStateToken()
    {
        return string.Join("|",
            _dateLabel?.Text ?? "",
            _embarkButton?.Visible ?? false,
            _embarkButton?.IsEnabled ?? false,
            _unreadyButton?.Visible ?? false,
            _unreadyButton?.IsEnabled ?? false,
            _backButton?.IsEnabled ?? false,
            _scoreWarning?.Visible ?? false,
            _modifierControls.Count,
            string.Join("|", _modifierControls.Select(control => control.Visible ? GetModifierText(_modifierControls.IndexOf(control)) : "")));
    }

    private static void AddIfVisible(List<Control> controls, Control? control)
    {
        if (control != null && control.Visible)
            controls.Add(control);
    }

    private static bool IsVisible(Control? control)
    {
        return control != null && control.Visible;
    }

    private static string? GetButtonStatus(NClickableControl? control)
    {
        if (control == null || control.IsEnabled)
            return null;

        return Ui("DAILY_RUN.DISABLED");
    }

    private static void Activate(NClickableControl? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.EmitSignal(NClickableControl.SignalName.Released, control);
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }

    private static string Ui(string key, object vars)
    {
        return Message.Localized("ui", key, vars).Resolve();
    }
}
