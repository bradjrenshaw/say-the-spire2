using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Platform;
using SayTheSpire2.Buffers;
using SayTheSpire2.Help;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Character select lobby. Wraps the game's NCharacterSelectScreen and, on
/// the November beta, NBlitzScreen — a near-copy of it (same character
/// buttons, ascension panel and lobby callbacks) with a mode info button
/// added. The screen is held as a Control and its lobby read by reflection
/// because NBlitzScreen doesn't exist on stable.
/// </summary>
public class CharacterSelectGameScreen : GameScreen
{
    public const string BlitzScreenTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen";

    public static CharacterSelectGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly System.Reflection.PropertyInfo _lobbyProperty;
    // main_menu_ui key prefix of the game mode this lobby is for ("BLITZ"),
    // or null for the standard character select.
    private readonly string? _modeKey;
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
        var messages = new System.Collections.Generic.List<HelpMessage>
        {
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.DECREASE_ASCENSION", "Decrease Ascension"), "mega_view_deck_and_tab_left"),
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.INCREASE_ASCENSION", "Increase Ascension"), "mega_view_exhaust_pile_and_tab_right"),
        };
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
        _modeKey = screen.GetType().FullName == BlitzScreenTypeName ? "BLITZ" : null;
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
