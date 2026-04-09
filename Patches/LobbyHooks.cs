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
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuOpened",
            typeof(LobbyHooks), nameof(CharacterSelectOpenedPostfix), "CharacterSelect OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuClosed",
            typeof(LobbyHooks), nameof(CharacterSelectClosedPostfix), "CharacterSelect OnSubmenuClosed");
        // CleanUpLobby is called when a run starts (bypasses OnSubmenuClosed)
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "CleanUpLobby",
            typeof(LobbyHooks), nameof(CharacterSelectClosedPostfix), "CharacterSelect CleanUpLobby");

        // Multiplayer lobby hooks (IStartRunLobbyListener on NCharacterSelectScreen)
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerConnected",
            typeof(LobbyHooks), nameof(LobbyPlayerConnectedPostfix), "Lobby PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerChanged",
            typeof(LobbyHooks), nameof(LobbyPlayerChangedPostfix), "Lobby PlayerChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "RemotePlayerDisconnected",
            typeof(LobbyHooks), nameof(LobbyPlayerDisconnectedPostfix), "Lobby RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "LocalPlayerDisconnected",
            typeof(LobbyHooks), nameof(LobbyLocalDisconnectedPostfix), "Lobby LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnEmbarkPressed",
            typeof(LobbyHooks), nameof(LobbyEmbarkPostfix), "Lobby OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnUnreadyPressed",
            typeof(LobbyHooks), nameof(LobbyUnreadyPostfix), "Lobby OnUnreadyPressed");

        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "PlayerConnected",
            typeof(LobbyHooks), nameof(LoadLobbyPlayerConnectedPostfix), "LoadLobby PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "PlayerReadyChanged",
            typeof(LobbyHooks), nameof(LoadLobbyPlayerReadyChangedPostfix), "LoadLobby PlayerReadyChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "RemotePlayerDisconnected",
            typeof(LobbyHooks), nameof(LoadLobbyPlayerDisconnectedPostfix), "LoadLobby RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "LocalPlayerDisconnected",
            typeof(LobbyHooks), nameof(LoadLobbyLocalDisconnectedPostfix), "LoadLobby LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "OnEmbarkPressed",
            typeof(LobbyHooks), nameof(LoadLobbyEmbarkPostfix), "LoadLobby OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerLoadGameScreen), "OnUnreadyPressed",
            typeof(LobbyHooks), nameof(LoadLobbyUnreadyPostfix), "LoadLobby OnUnreadyPressed");
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
