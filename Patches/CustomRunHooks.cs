using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class CustomRunHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NCustomRunScreen), "OnSubmenuOpened",
            nameof(CustomRunOpenedPostfix), "CustomRun OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "OnSubmenuClosed",
            nameof(CustomRunClosedPostfix), "CustomRun OnSubmenuClosed");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "CleanUpLobby",
            nameof(CustomRunClosedPostfix), "CustomRun CleanUpLobby");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "PlayerConnected",
            nameof(CustomRunPlayerConnectedPostfix), "CustomRun PlayerConnected");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "PlayerChanged",
            nameof(CustomRunPlayerChangedPostfix), "CustomRun PlayerChanged");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "RemotePlayerDisconnected",
            nameof(CustomRunPlayerDisconnectedPostfix), "CustomRun RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "LocalPlayerDisconnected",
            nameof(CustomRunLocalDisconnectedPostfix), "CustomRun LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "AscensionChanged",
            nameof(CustomRunStateChangedPostfix), "CustomRun AscensionChanged");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "SeedChanged",
            nameof(CustomRunStateChangedPostfix), "CustomRun SeedChanged");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "ModifiersChanged",
            nameof(CustomRunStateChangedPostfix), "CustomRun ModifiersChanged");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "OnEmbarkPressed",
            nameof(CustomRunStateChangedPostfix), "CustomRun OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NCustomRunScreen), "OnUnreadyPressed",
            nameof(CustomRunStateChangedPostfix), "CustomRun OnUnreadyPressed");

        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnSubmenuOpened",
            nameof(CustomRunLoadOpenedPostfix), "CustomRunLoad OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnSubmenuClosed",
            nameof(CustomRunLoadClosedPostfix), "CustomRunLoad OnSubmenuClosed");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "CleanUpLobby",
            nameof(CustomRunLoadClosedPostfix), "CustomRunLoad CleanUpLobby");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "PlayerConnected",
            nameof(CustomRunLoadPlayerConnectedPostfix), "CustomRunLoad PlayerConnected");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "PlayerReadyChanged",
            nameof(CustomRunLoadPlayerReadyChangedPostfix), "CustomRunLoad PlayerReadyChanged");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "RemotePlayerDisconnected",
            nameof(CustomRunLoadPlayerDisconnectedPostfix), "CustomRunLoad RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "LocalPlayerDisconnected",
            nameof(CustomRunLoadLocalDisconnectedPostfix), "CustomRunLoad LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "BeginRun",
            nameof(CustomRunLoadStateChangedPostfix), "CustomRunLoad BeginRun");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnEmbarkPressed",
            nameof(CustomRunLoadEmbarkPostfix), "CustomRunLoad OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnUnreadyPressed",
            nameof(CustomRunLoadUnreadyPostfix), "CustomRunLoad OnUnreadyPressed");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(CustomRunHooks), handlerName, label, isPrefix);
    }

    public static void CustomRunOpenedPostfix(NCustomRunScreen __instance)
    {
        if (CustomRunGameScreen.Current == null)
            ScreenManager.PushScreen(new CustomRunGameScreen(__instance));
    }

    public static void CustomRunClosedPostfix()
    {
        if (CustomRunGameScreen.Current != null)
            ScreenManager.RemoveScreen(CustomRunGameScreen.Current);
    }

    public static void CustomRunPlayerConnectedPostfix(LobbyPlayer player)
    {
        CustomRunGameScreen.Current?.OnLobbyUpdated();
    }

    public static void CustomRunPlayerChangedPostfix(LobbyPlayer player)
    {
        CustomRunGameScreen.Current?.OnLobbyUpdated();
    }

    public static void CustomRunPlayerDisconnectedPostfix(LobbyPlayer player)
    {
        CustomRunGameScreen.Current?.OnLobbyUpdated();
    }

    public static void CustomRunLocalDisconnectedPostfix(NetErrorInfo info)
    {
        CustomRunGameScreen.Current?.OnLobbyUpdated();
    }

    public static void CustomRunStateChangedPostfix()
    {
        CustomRunGameScreen.Current?.OnStateChanged();
    }

    public static void CustomRunLoadOpenedPostfix(NCustomRunLoadScreen __instance)
    {
        if (CustomRunLoadGameScreen.Current == null)
            ScreenManager.PushScreen(new CustomRunLoadGameScreen(__instance));
    }

    public static void CustomRunLoadClosedPostfix()
    {
        if (CustomRunLoadGameScreen.Current != null)
            ScreenManager.RemoveScreen(CustomRunLoadGameScreen.Current);
    }

    public static void CustomRunLoadPlayerConnectedPostfix(ulong playerId)
    {
        CustomRunLoadGameScreen.Current?.OnPlayerConnected(playerId);
    }

    public static void CustomRunLoadPlayerReadyChangedPostfix(ulong playerId)
    {
        CustomRunLoadGameScreen.Current?.OnPlayerReadyChanged(playerId);
    }

    public static void CustomRunLoadPlayerDisconnectedPostfix(ulong playerId)
    {
        CustomRunLoadGameScreen.Current?.OnPlayerDisconnected(playerId);
    }

    public static void CustomRunLoadLocalDisconnectedPostfix(NetErrorInfo info)
    {
        CustomRunLoadGameScreen.Current?.OnLocalDisconnected();
    }

    public static void CustomRunLoadEmbarkPostfix()
    {
        CustomRunLoadGameScreen.Current?.OnEmbarkPressed();
    }

    public static void CustomRunLoadUnreadyPostfix()
    {
        CustomRunLoadGameScreen.Current?.OnUnreadyPressed();
    }

    public static void CustomRunLoadStateChangedPostfix()
    {
        CustomRunLoadGameScreen.Current?.OnStateChanged();
    }
}
