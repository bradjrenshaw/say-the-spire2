using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class LobbyHooks
{
    private static readonly System.Reflection.FieldInfo? LoadRunLobbyField =
        AccessTools.Field(typeof(NMultiplayerLoadGameScreen), "_runLobby");

    public static void Initialize(Harmony harmony)
    {
        // Character select hooks
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuOpened",
            nameof(CharacterSelectOpenedPostfix), "CharacterSelect OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuClosed",
            nameof(CharacterSelectClosedPostfix), "CharacterSelect OnSubmenuClosed");
        // CleanUpLobby is called when a run starts (bypasses OnSubmenuClosed)
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "CleanUpLobby",
            nameof(CharacterSelectClosedPostfix), "CharacterSelect CleanUpLobby");

        // Multiplayer lobby hooks (IStartRunLobbyListener on NCharacterSelectScreen)
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerConnected",
            nameof(LobbyPlayerConnectedPostfix), "Lobby PlayerConnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerChanged",
            nameof(LobbyPlayerChangedPostfix), "Lobby PlayerChanged");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "RemotePlayerDisconnected",
            nameof(LobbyPlayerDisconnectedPostfix), "Lobby RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "LocalPlayerDisconnected",
            nameof(LobbyLocalDisconnectedPostfix), "Lobby LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnEmbarkPressed",
            nameof(LobbyEmbarkPostfix), "Lobby OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnUnreadyPressed",
            nameof(LobbyUnreadyPostfix), "Lobby OnUnreadyPressed");

        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "PlayerConnected",
            nameof(LoadLobbyPlayerConnectedPostfix), "LoadLobby PlayerConnected");
        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "PlayerReadyChanged",
            nameof(LoadLobbyPlayerReadyChangedPostfix), "LoadLobby PlayerReadyChanged");
        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "RemotePlayerDisconnected",
            nameof(LoadLobbyPlayerDisconnectedPostfix), "LoadLobby RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "LocalPlayerDisconnected",
            nameof(LoadLobbyLocalDisconnectedPostfix), "LoadLobby LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "OnEmbarkPressed",
            nameof(LoadLobbyEmbarkPostfix), "LoadLobby OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "OnUnreadyPressed",
            nameof(LoadLobbyUnreadyPostfix), "LoadLobby OnUnreadyPressed");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(LobbyHooks), handlerName, label, isPrefix);
    }

    public static void CharacterSelectOpenedPostfix(NCharacterSelectScreen __instance)
    {
        if (CharacterSelectGameScreen.Current == null)
            ScreenManager.PushScreen(new CharacterSelectGameScreen(__instance));
    }

    public static void CharacterSelectClosedPostfix()
    {
        if (CharacterSelectGameScreen.Current != null)
            ScreenManager.RemoveScreen(CharacterSelectGameScreen.Current);
    }

    public static void LobbyPlayerConnectedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerConnected(__instance, player);
    }

    public static void LobbyPlayerChangedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerChanged(__instance, player);
    }

    public static void LobbyPlayerDisconnectedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerDisconnected(__instance, player);
    }

    public static void LobbyLocalDisconnectedPostfix(NCharacterSelectScreen __instance, NetErrorInfo info)
    {
        CharacterSelectGameScreen.Current?.OnLobbyLocalDisconnected(__instance, info);
    }

    public static void LobbyEmbarkPostfix(NCharacterSelectScreen __instance)
    {
        CharacterSelectGameScreen.Current?.OnLobbyStateChanged();
    }

    public static void LobbyUnreadyPostfix(NCharacterSelectScreen __instance)
    {
        CharacterSelectGameScreen.Current?.OnLobbyStateChanged();
    }

    public static void LoadLobbyPlayerConnectedPostfix(NMultiplayerLoadGameScreen __instance, ulong playerId)
    {
        var lobby = GetLoadLobby(__instance);
        if (!IsMultiplayer(lobby))
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_JOINED", new
        {
            player = MultiplayerHelper.GetPlayerName(playerId, lobby.NetService.Platform)
        }));
    }

    public static void LoadLobbyPlayerReadyChangedPostfix(NMultiplayerLoadGameScreen __instance, ulong playerId)
    {
        var lobby = GetLoadLobby(__instance);
        if (!IsMultiplayer(lobby))
            return;

        if (playerId == lobby.NetService.NetId)
            return;

        var status = lobby.IsPlayerReady(playerId)
            ? Ui("DAILY_RUN.READY")
            : Ui("DAILY_RUN.NOT_READY");

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOAD_LOBBY_CHANGED", new
        {
            player = MultiplayerHelper.GetPlayerName(playerId, lobby.NetService.Platform),
            status,
        }));
    }

    public static void LoadLobbyPlayerDisconnectedPostfix(NMultiplayerLoadGameScreen __instance, ulong playerId)
    {
        var lobby = GetLoadLobby(__instance);
        if (!IsMultiplayer(lobby))
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_LEFT", new
        {
            player = MultiplayerHelper.GetPlayerName(playerId, lobby.NetService.Platform)
        }));
    }

    public static void LoadLobbyLocalDisconnectedPostfix(NMultiplayerLoadGameScreen __instance, NetErrorInfo info)
    {
        var lobby = GetLoadLobby(__instance);
        if (IsMultiplayer(lobby) && !(info.SelfInitiated && info.GetReason() == NetError.Quit))
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_DISCONNECTED"));
    }

    public static void LoadLobbyEmbarkPostfix(NMultiplayerLoadGameScreen __instance)
    {
        var lobby = GetLoadLobby(__instance);
        if (IsMultiplayer(lobby))
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.MARKED_READY"));
    }

    public static void LoadLobbyUnreadyPostfix(NMultiplayerLoadGameScreen __instance)
    {
        var lobby = GetLoadLobby(__instance);
        if (IsMultiplayer(lobby))
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.NO_LONGER_READY"));
    }

    private static LoadRunLobby? GetLoadLobby(NMultiplayerLoadGameScreen screen)
    {
        return LoadRunLobbyField?.GetValue(screen) as LoadRunLobby;
    }

    private static bool IsMultiplayer(LoadRunLobby? lobby)
    {
        return lobby?.NetService.Type is NetGameType.Host or NetGameType.Client;
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
