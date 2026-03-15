using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Platform;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CharacterSelectGameScreen : GameScreen
{
    public static CharacterSelectGameScreen? Current { get; private set; }

    private readonly NCharacterSelectScreen _screen;
    private int _lastAscension = -1;
    private bool _isMultiplayer;
    private bool _lastLocalReady;

    public override string? ScreenName => "Character Select";
    public override System.Collections.Generic.IEnumerable<string> AlwaysEnabledBuffers =>
        _isMultiplayer ? new[] { "lobby" } : System.Array.Empty<string>();

    public CharacterSelectGameScreen(NCharacterSelectScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        Current = this;
        base.OnPush();

        try
        {
            var lobby = _screen.Lobby;
            _isMultiplayer = lobby?.NetService.Type.IsMultiplayer() == true;

            if (_isMultiplayer)
            {
                _lastLocalReady = lobby!.LocalPlayer.isReady;
                var lobbyBuffer = BufferManager.Instance.GetBuffer("lobby") as LobbyBuffer;
                if (lobbyBuffer != null)
                {
                    lobbyBuffer.Bind(lobby!);
                    lobbyBuffer.Update();
                    BufferManager.Instance.EnableBuffer("lobby", true);
                }
            }
        }
        catch { }
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

        // Ascension polling
        var panel = _screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        if (panel != null)
        {
            var current = panel.Ascension;
            if (_lastAscension == -1)
            {
                _lastAscension = current;
            }
            else if (current != _lastAscension)
            {
                _lastAscension = current;
                var title = AscensionHelper.GetTitle(current).GetFormattedText();
                var description = AscensionHelper.GetDescription(current).GetFormattedText();
                SpeechManager.Output(Message.Raw($"Ascension {current}: {title}. {description}"));
            }
        }

        // Multiplayer ready state polling
        if (_isMultiplayer)
        {
            try
            {
                var lobby = _screen.Lobby;
                if (lobby == null) return;

                var localReady = lobby.LocalPlayer.isReady;
                if (localReady != _lastLocalReady)
                {
                    _lastLocalReady = localReady;
                    if (localReady)
                        SpeechManager.Output(Message.Raw("Marked as ready, waiting for other players"));
                    else
                        SpeechManager.Output(Message.Raw("No longer ready"));
                }
            }
            catch { }
        }
    }

    // Multiplayer lobby callbacks (called from ScreenHooks postfixes)

    public void OnLobbyPlayerConnected(NCharacterSelectScreen screen, LobbyPlayer player)
    {
        if (!_isMultiplayer) return;
        var name = GetPlayerName(player.id);
        SpeechManager.Output(Message.Raw($"{name} joined the lobby"));
        UpdateLobbyBuffer();
    }

    public void OnLobbyPlayerChanged(NCharacterSelectScreen screen, LobbyPlayer player)
    {
        if (!_isMultiplayer) return;
        try
        {
            // Skip local player changes — handled by polling
            if (player.id == screen.Lobby.LocalPlayer.id) return;
        }
        catch { }

        var name = GetPlayerName(player.id);
        var charName = player.character?.Title?.GetFormattedText() ?? "No character";
        var readyStr = player.isReady ? "Ready" : "Not ready";
        SpeechManager.Output(Message.Raw($"{name}: {charName}, {readyStr}"));
        UpdateLobbyBuffer();
    }

    public void OnLobbyPlayerDisconnected(NCharacterSelectScreen screen, LobbyPlayer player)
    {
        if (!_isMultiplayer) return;
        var name = GetPlayerName(player.id);
        SpeechManager.Output(Message.Raw($"{name} left the lobby"));
        UpdateLobbyBuffer();
    }

    public void OnLobbyLocalDisconnected(NCharacterSelectScreen screen, NetErrorInfo info)
    {
        if (!_isMultiplayer) return;
        SpeechManager.Output(Message.Raw("Disconnected from lobby"));
    }

    private string GetPlayerName(ulong playerId)
    {
        try
        {
            var lobby = _screen.Lobby;
            if (lobby != null)
                return PlatformUtil.GetPlayerName(lobby.NetService.Platform, playerId);
        }
        catch { }
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
            ContainerLabel = _isMultiplayer ? "Lobby" : "Characters",
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

        for (int i = 0; i < charButtons.Count; i++)
        {
            var self = charButtons[i].GetPath();
            charButtons[i].FocusNeighborLeft = i > 0 ? charButtons[i - 1].GetPath() : self;
            charButtons[i].FocusNeighborRight = i < charButtons.Count - 1 ? charButtons[i + 1].GetPath() : self;
            charButtons[i].FocusNeighborTop = self;
            charButtons[i].FocusNeighborBottom = self;
        }

        MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] Lobby BuildRegistry: {charButtons.Count} controls");
    }
}
