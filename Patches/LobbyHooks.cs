using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class LobbyHooks
{
    public static void Initialize(Harmony harmony)
    {
        // Character select hooks
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuOpened",
            nameof(CharacterSelectOpenedPostfix), "CharacterSelect OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuClosed",
            nameof(CharacterSelectClosedPostfix), "CharacterSelect OnSubmenuClosed");

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
}
