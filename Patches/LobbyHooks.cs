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
        PatchCharacterSelect(harmony, typeof(NCharacterSelectScreen), "CharacterSelect");

        // Beta-only: the Blitz lobby is a near-copy of character select
        // with the same listener methods. Absent on stable, so resolved by
        // name and skipped when missing.
        if (AccessTools.TypeByName(CharacterSelectGameScreen.BlitzScreenTypeName) is { } blitzScreen)
            PatchCharacterSelect(harmony, blitzScreen, "Blitz");

        // Likewise the Dread lobby: same buttons and listener methods, with
        // win streaks in place of the info and ascension panels.
        if (AccessTools.TypeByName(Views.DreadLobbyView.ScreenTypeName) is { } dreadScreen)
            PatchCharacterSelect(harmony, dreadScreen, "Dread");

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

    private static void PatchCharacterSelect(Harmony harmony, System.Type screen, string label)
    {
        HarmonyHelper.PatchIfFound(harmony, screen, "OnSubmenuOpened",
            typeof(LobbyHooks), nameof(CharacterSelectOpenedPostfix), $"{label} OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, screen, "OnSubmenuClosed",
            typeof(LobbyHooks), nameof(CharacterSelectClosedPostfix), $"{label} OnSubmenuClosed");
        // CleanUpLobby is called when a run starts (bypasses OnSubmenuClosed)
        HarmonyHelper.PatchIfFound(harmony, screen, "CleanUpLobby",
            typeof(LobbyHooks), nameof(CharacterSelectClosedPostfix), $"{label} CleanUpLobby");

        // Multiplayer lobby hooks (IStartRunLobbyListener)
        HarmonyHelper.PatchIfFound(harmony, screen, "PlayerConnected",
            typeof(LobbyHooks), nameof(LobbyPlayerConnectedPostfix), $"{label} PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, screen, "PlayerChanged",
            typeof(LobbyHooks), nameof(LobbyPlayerChangedPostfix), $"{label} PlayerChanged");
        HarmonyHelper.PatchIfFound(harmony, screen, "RemotePlayerDisconnected",
            typeof(LobbyHooks), nameof(LobbyPlayerDisconnectedPostfix), $"{label} RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, screen, "LocalPlayerDisconnected",
            typeof(LobbyHooks), nameof(LobbyLocalDisconnectedPostfix), $"{label} LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, screen, "OnUnreadyPressed",
            typeof(LobbyHooks), nameof(LobbyUnreadyPostfix), $"{label} OnUnreadyPressed");
    }

    // __instance is Control: these patch NCharacterSelectScreen and the
    // beta's NBlitzScreen and NDreadScreen.
    public static void CharacterSelectOpenedPostfix(Godot.Control __instance)
    {
        if (CharacterSelectGameScreen.Current == null)
            ScreenManager.PushScreen(new CharacterSelectGameScreen(__instance));
    }

    public static void CharacterSelectClosedPostfix()
    {
        if (CharacterSelectGameScreen.Current != null)
            ScreenManager.RemoveScreen(CharacterSelectGameScreen.Current);
    }

    // Player params are object: the struct type is branch-divergent
    // (LobbyPlayer on stable, StartRunLobbyPlayer on beta), so Harmony boxes
    // it for us and LobbyPlayerView reads the shared field names.
    public static void LobbyPlayerConnectedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            CharacterSelectGameScreen.Current?.OnLobbyPlayerConnected(view);
    }

    public static void LobbyPlayerChangedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            CharacterSelectGameScreen.Current?.OnLobbyPlayerChanged(view);
    }

    public static void LobbyPlayerDisconnectedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            CharacterSelectGameScreen.Current?.OnLobbyPlayerDisconnected(view);
    }

    public static void LobbyLocalDisconnectedPostfix(NetErrorInfo info)
    {
        CharacterSelectGameScreen.Current?.OnLobbyLocalDisconnected(info);
    }

    public static void LobbyUnreadyPostfix()
    {
        CharacterSelectGameScreen.Current?.OnLobbyStateChanged();
    }

    // __0 is object: the argument is `ulong playerId` on stable and
    // `LoadRunLobbyPlayer player` on the November beta, so it is matched by
    // position (the names differ) and boxed; LobbyPlayerView.IdOf reads either.
    public static void LoadLobbyPlayerConnectedPostfix(NMultiplayerLoadGameScreen __instance, object __0)
    {
        var playerId = Views.LobbyPlayerView.IdOf(__0);
        var lobby = GetLoadLobby(__instance);
        if (!IsMultiplayer(lobby))
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_JOINED", new
        {
            player = MultiplayerHelper.GetPlayerName(playerId, lobby?.NetService.Platform)
        }));
    }

    public static void LoadLobbyPlayerReadyChangedPostfix(NMultiplayerLoadGameScreen __instance, ulong playerId)
    {
        var lobby = GetLoadLobby(__instance);
        if (!IsMultiplayer(lobby))
            return;

        if (playerId == lobby?.NetService.NetId)
            return;

        var status = lobby?.IsPlayerReady(playerId) == true
            ? Ui("DAILY_RUN.READY")
            : Ui("DAILY_RUN.NOT_READY");

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOAD_LOBBY_CHANGED", new
        {
            player = MultiplayerHelper.GetPlayerName(playerId, lobby?.NetService.Platform),
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
            player = MultiplayerHelper.GetPlayerName(playerId, lobby?.NetService.Platform)
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
