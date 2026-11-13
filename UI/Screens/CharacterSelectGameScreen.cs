using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Platform;
using SayTheSpire2.Buffers;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Character select lobby. Wraps the game's NCharacterSelectScreen and, on
/// the November beta, NBlitzScreen — a near-copy of it (same character
/// buttons, ascension panel and lobby callbacks) with a mode info button
/// added — and NDreadScreen, which has the same buttons and callbacks but
/// win streaks and a "Begin Rotating Streak" tickbox in place of the info and
/// ascension panels (see <see cref="Views.DreadLobbyView"/>). The screen is
/// held as a Control and its lobby read by reflection because the mode
/// screens don't exist on stable.
/// </summary>
public class CharacterSelectGameScreen : GameScreen
{
    public const string BlitzScreenTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen";

    public static CharacterSelectGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly System.Reflection.PropertyInfo _lobbyProperty;
    // main_menu_ui key prefix of the game mode this lobby is for ("BLITZ",
    // "DREAD"), or null for the standard character select.
    private readonly string? _modeKey;
    // Set only for the Dread lobby.
    private readonly Views.DreadLobbyView? _dread;
    private int _dreadButtonCount = -1;
    private Control? _wiredButton;
    private bool _wiredTickboxUsable;
    private bool _checkedUnderConstruction;
    private int _lastAscension = -1;
    private Control? _lastFocusedButton;
    private bool _isMultiplayer;
    private bool _lastLocalReady;

    private StartRunLobby? Lobby => _lobbyProperty.GetValue(_screen) as StartRunLobby;

    // Mode lobbies are named as the mode select button names them.
    public override Message? ScreenName => _modeKey != null
        ? Message.Raw(new LocString("main_menu_ui", _modeKey + ".title").GetFormattedText())
        : Message.Localized("ui", "SCREENS.CHARACTER_SELECT");
    public override System.Collections.Generic.IEnumerable<string> AlwaysEnabledBuffers =>
        _isMultiplayer ? new[] { "lobby" } : System.Array.Empty<string>();

    public override System.Collections.Generic.List<HelpMessage> GetHelpMessages()
    {
        var messages = new System.Collections.Generic.List<HelpMessage>();
        // The Dread lobby has no ascension control (always 10).
        if (_dread == null)
        {
            messages.Add(new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.DECREASE_ASCENSION", "Decrease Ascension"), "mega_view_deck_and_tab_left"));
            messages.Add(new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.INCREASE_ASCENSION", "Increase Ascension"), "mega_view_exhaust_pile_and_tab_right"));
        }
        // The mode's info button is on the game's confirm action, which our
        // Accept injects.
        if (_modeKey != null)
            messages.Add(new ControlHelpMessage(
                LocalizationManager.GetOrDefault("ui", "HELP.MODE_INFO", "Mode Info"), "ui_accept"));
        return messages;
    }

    public CharacterSelectGameScreen(Control screen)
    {
        _screen = screen;
        _lobbyProperty = AccessTools.Property(screen.GetType(), "Lobby")!;
        _dread = Views.DreadLobbyView.FromScreen(screen);
        _modeKey = _dread != null ? "DREAD"
            : screen.GetType().FullName == BlitzScreenTypeName ? "BLITZ"
            : null;

        // The game's select action embarks from anywhere on this screen, and
        // also clicks whatever is focused — on the rotating-streak tickbox it
        // would do both. Take it over there and only toggle the tickbox.
        if (_dread != null)
            ClaimAction("ui_select", condition: IsDreadTickboxFocused);
    }

    private bool IsDreadTickboxFocused()
    {
        if (!GodotObject.IsInstanceValid(_screen)) return false;
        var tickbox = _dread?.Tickbox;
        return tickbox != null && tickbox.HasFocus() && _dread?.IsTickboxUsable == true;
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key != "ui_select" || !IsDreadTickboxFocused())
            return false;
        // ProxyCheckbox announces the new state from the tickbox's Toggled signal.
        _dread?.Tickbox?.ForceClick();
        return true;
    }

    public override void OnPush()
    {
        Current = this;
        base.OnPush();

        try
        {
            var lobby = Lobby;
            _isMultiplayer = lobby?.NetService.Type.IsMultiplayer() == true;

            if (_isMultiplayer)
            {
                _lastLocalReady = Views.LobbyPlayerView.LocalPlayerOf(lobby)?.IsReady == true;
                var lobbyBuffer = BufferManager.Instance.GetBuffer("lobby") as LobbyBuffer;
                if (lobbyBuffer != null)
                {
                    lobbyBuffer.Bind(lobby!);
                    lobbyBuffer.Update();
                    BufferManager.Instance.EnableBuffer("lobby", true);
                }
            }
        }
        catch (Exception e) { MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Character select lobby init failed: {e.Message}"); }
    }

    public override void OnPop()
    {
        base.OnPop();

        if (_isMultiplayer)
        {
            var lobbyBuffer = BufferManager.Instance.GetBuffer("lobby") as LobbyBuffer;
            if (lobbyBuffer != null)
            {
                lobbyBuffer.Enabled = false;
            }
        }

        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen)) return;

        AnnounceUnderConstructionOnce();
        UpdateDreadLobby();

        // Ascension polling — only announce when the user changes ascension (D/F),
        // not when switching characters causes the displayed ascension to change.
        var focusedButton = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        var panel = _screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        if (panel != null)
        {
            var current = panel.Ascension;
            if (_lastAscension == -1 || focusedButton != _lastFocusedButton)
            {
                // Initial load or character changed — update silently
                _lastAscension = current;
            }
            else if (current != _lastAscension)
            {
                _lastAscension = current;
                var title = AscensionText.Title(current, panel);
                var description = AscensionText.Description(current, panel);
                SpeechManager.Output(Message.Localized("ui", "SPEECH.ASCENSION_CHANGED", new { value = current, title, description }));
            }
        }
        _lastFocusedButton = focusedButton;

        // Multiplayer ready state polling
        if (_isMultiplayer)
        {
            try
            {
                var lobby = Lobby;
                if (lobby == null) return;

                var localReady = Views.LobbyPlayerView.LocalPlayerOf(lobby)?.IsReady == true;
                if (localReady != _lastLocalReady)
                {
                    _lastLocalReady = localReady;
                    if (localReady)
                        SpeechManager.Output(Message.Localized("ui", "SPEECH.MARKED_READY"));
                    else
                        SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_LONGER_READY"));
                }
            }
            catch (Exception e) { MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Multiplayer ready state poll failed: {e.Message}"); }
        }
    }

    // The Blitz lobby shows an "Under Construction" badge in singleplayer
    // (the game hides it once the multiplayer player list is shown). Spoken
    // once per visit, with the game's own text.
    private void AnnounceUnderConstructionOnce()
    {
        if (_checkedUnderConstruction) return;
        _checkedUnderConstruction = true;

        if (_screen.GetNodeOrNull<Control>("%UnderConstruction") is not { } badge || !badge.IsVisibleInTree())
            return;
        var text = badge.GetNodeOrNull<RichTextLabel>("%Label")?.Text;
        if (!string.IsNullOrWhiteSpace(text))
            SpeechManager.Output(Message.Raw(ProxyElement.StripBbcode(text).Trim()));
    }

    // Dread lobby upkeep, every frame.
    private void UpdateDreadLobby()
    {
        if (_dread == null) return;

        var container = _screen.GetNodeOrNull<Control>("CharSelectButtons/ButtonContainer");
        if (container == null) return;

        // The Rotating button appears and disappears with the rotating
        // streak (the game re-decides after the streak animation), so the
        // character list is rebuilt when the visible buttons change.
        var visibleButtons = container.GetChildren().OfType<NCharacterSelectButton>().Count(b => b.Visible);
        if (_dreadButtonCount != -1 && visibleButtons != _dreadButtonCount)
        {
            ClearRegistry();
            BuildRegistry();
        }
        _dreadButtonCount = visibleButtons;

        // The game gives the tickbox no hotkey and no place in the focus
        // order (every character button's up and down lead back to itself),
        // so it is mouse-only. Make Down from the focused character reach it
        // and Up come back, while the game has it showing and enabled.
        var tickbox = _dread.Tickbox;
        if (tickbox == null) return;

        // Its scene sets no focus mode (unlike the game's other tickboxes),
        // so it can't take focus at all, and the game puts it back to
        // unfocusable every time it re-enables it. Keep it focusable.
        if (tickbox.FocusMode != Control.FocusModeEnum.All)
            tickbox.FocusMode = Control.FocusModeEnum.All;

        if (_screen.GetViewport()?.GuiGetFocusOwner() is not NCharacterSelectButton focused
            || focused.GetParent() != container)
            return;

        var usable = _dread.IsTickboxUsable;
        if (focused == _wiredButton && usable == _wiredTickboxUsable)
            return;
        _wiredButton = focused;
        _wiredTickboxUsable = usable;

        var buttonPath = focused.GetPath();
        focused.FocusNeighborBottom = usable ? tickbox.GetPath() : buttonPath;
        tickbox.FocusNeighborTop = buttonPath;
    }

    // Multiplayer lobby callbacks (called from ScreenHooks postfixes)

    public void OnLobbyPlayerConnected(Views.LobbyPlayerView player)
    {
        if (!_isMultiplayer) return;
        var name = GetPlayerName(player.Id);
        SpeechManager.Output(Message.Localized("ui", "SPEECH.PLAYER_JOINED", new { name }));
        UpdateLobbyBuffer();
    }

    public void OnLobbyPlayerChanged(Views.LobbyPlayerView player)
    {
        if (!_isMultiplayer) return;
        try
        {
            // Skip local player changes — handled by polling
            if (player.Id == Views.LobbyPlayerView.LocalPlayerOf(Lobby)?.Id) return;
        }
        catch (Exception e) { MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Lobby local player check failed: {e.Message}"); }

        var name = GetPlayerName(player.Id);
        var charName = player.Character?.Title?.GetFormattedText() ?? LocalizationManager.GetOrDefault("ui", "DAILY_RUN.NO_CHARACTER", "No character");
        var readyStr = player.IsReady
            ? LocalizationManager.GetOrDefault("ui", "DAILY_RUN.READY", "Ready")
            : LocalizationManager.GetOrDefault("ui", "DAILY_RUN.NOT_READY", "Not ready");
        SpeechManager.Output(Message.Localized("ui", "SPEECH.PLAYER_CHANGED", new { name, character = charName, status = readyStr }));
        UpdateLobbyBuffer();
    }

    public void OnLobbyPlayerDisconnected(Views.LobbyPlayerView player)
    {
        if (!_isMultiplayer) return;
        var name = GetPlayerName(player.Id);
        SpeechManager.Output(Message.Localized("ui", "SPEECH.PLAYER_LEFT", new { name }));
        UpdateLobbyBuffer();
    }

    public void OnLobbyLocalDisconnected(NetErrorInfo info)
    {
        if (!_isMultiplayer) return;
        SpeechManager.Output(Message.Localized("ui", "SPEECH.DISCONNECTED"));
    }

    private string GetPlayerName(ulong playerId)
    {
        try
        {
            return MultiplayerHelper.GetPlayerName(playerId, Lobby?.NetService.Platform);
        }
        catch (Exception e) { MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Player name lookup failed: {e.Message}"); }
        return $"Player {playerId}";
    }

    public void OnLobbyStateChanged()
    {
        ClearRegistry();
        BuildRegistry();

        // After unready, grab focus on the first character button
        var container = _screen.GetNodeOrNull<Control>("CharSelectButtons/ButtonContainer");
        if (container != null)
        {
            var firstButton = container.GetChildren().OfType<NCharacterSelectButton>()
                .FirstOrDefault(b => b.Visible && b.FocusMode != Control.FocusModeEnum.None);
            firstButton?.GrabFocus();
        }

        MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] Lobby state changed, rebuilt registry");
    }

    private void UpdateLobbyBuffer()
    {
        var buffer = BufferManager.Instance.GetBuffer("lobby") as LobbyBuffer;
        buffer?.Update();
    }

    protected override void BuildRegistry()
    {
        var list = new ListContainer
        {
            ContainerLabel = _isMultiplayer
                ? Message.Localized("ui", "CONTAINERS.LOBBY")
                : Message.Localized("ui", "CONTAINERS.CHARACTERS"),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = list;

        // Character buttons (may be disabled in waiting state but still visible)
        var container = _screen.GetNodeOrNull<Control>("CharSelectButtons/ButtonContainer");
        if (container != null)
        {
            foreach (var button in container.GetChildren().OfType<NCharacterSelectButton>().Where(b => b.Visible))
            {
                var proxy = new ProxyCharacterButton(button);
                list.Add(proxy);
                Register(button, proxy);
            }
        }

        // Only register character buttons — lobby actions (embark, back, unready)
        // are handled via controller hotkeys (A/B), not focus navigation.
        // Invite button is mouse-only.

        // Set up focus navigation for character buttons only
        var charButtons = new System.Collections.Generic.List<Control>();
        foreach (var (ctrl, _) in GetRegisteredControls())
            charButtons.Add(ctrl);

        // Dread, singleplayer: the rotating-streak tickbox, reached with Down
        // from a character (wired in UpdateDreadLobby). Always registered;
        // the proxy drops out of the list positions while the game hides it.
        if (_dread != null && !_isMultiplayer && _dread.Tickbox is { } tickbox)
        {
            var tickboxProxy = new ProxyDreadRotationTickbox(tickbox, _dread);
            list.Add(tickboxProxy);
            Register(tickbox, tickboxProxy);

            var self = tickbox.GetPath();
            tickbox.FocusNeighborLeft = self;
            tickbox.FocusNeighborRight = self;
            tickbox.FocusNeighborBottom = self;
        }
        // The rebuild below resets the buttons' down neighbours.
        _wiredButton = null;

        if (Settings.UIEnhancementsSettings.CharacterSelect.Get())
        {
            for (int i = 0; i < charButtons.Count; i++)
            {
                var self = charButtons[i].GetPath();
                charButtons[i].FocusNeighborLeft = i > 0 ? charButtons[i - 1].GetPath() : self;
                charButtons[i].FocusNeighborRight = i < charButtons.Count - 1 ? charButtons[i + 1].GetPath() : self;
                charButtons[i].FocusNeighborTop = self;
                charButtons[i].FocusNeighborBottom = self;
            }
        }

        MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] Lobby BuildRegistry: {charButtons.Count} controls");
    }
}
