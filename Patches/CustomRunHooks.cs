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
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "OnSubmenuOpened",
            typeof(CustomRunHooks), nameof(CustomRunOpenedPostfix), "CustomRun OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "OnSubmenuClosed",
            typeof(CustomRunHooks), nameof(CustomRunClosedPostfix), "CustomRun OnSubmenuClosed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "CleanUpLobby",
            typeof(CustomRunHooks), nameof(CustomRunClosedPostfix), "CustomRun CleanUpLobby");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "PlayerConnected",
            typeof(CustomRunHooks), nameof(CustomRunPlayerConnectedPostfix), "CustomRun PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "PlayerChanged",
            typeof(CustomRunHooks), nameof(CustomRunPlayerChangedPostfix), "CustomRun PlayerChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "RemotePlayerDisconnected",
            typeof(CustomRunHooks), nameof(CustomRunPlayerDisconnectedPostfix), "CustomRun RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "LocalPlayerDisconnected",
            typeof(CustomRunHooks), nameof(CustomRunLocalDisconnectedPostfix), "CustomRun LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "AscensionChanged",
            typeof(CustomRunHooks), nameof(CustomRunStateChangedPostfix), "CustomRun AscensionChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "SeedChanged",
            typeof(CustomRunHooks), nameof(CustomRunStateChangedPostfix), "CustomRun SeedChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "ModifiersChanged",
            typeof(CustomRunHooks), nameof(CustomRunStateChangedPostfix), "CustomRun ModifiersChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "OnEmbarkPressed",
            typeof(CustomRunHooks), nameof(CustomRunStateChangedPostfix), "CustomRun OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunScreen), "OnUnreadyPressed",
            typeof(CustomRunHooks), nameof(CustomRunStateChangedPostfix), "CustomRun OnUnreadyPressed");

        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnSubmenuOpened",
            typeof(CustomRunHooks), nameof(CustomRunLoadOpenedPostfix), "CustomRunLoad OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnSubmenuClosed",
            typeof(CustomRunHooks), nameof(CustomRunLoadClosedPostfix), "CustomRunLoad OnSubmenuClosed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "CleanUpLobby",
            typeof(CustomRunHooks), nameof(CustomRunLoadClosedPostfix), "CustomRunLoad CleanUpLobby");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "PlayerConnected",
            typeof(CustomRunHooks), nameof(CustomRunLoadPlayerConnectedPostfix), "CustomRunLoad PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "PlayerReadyChanged",
            typeof(CustomRunHooks), nameof(CustomRunLoadPlayerReadyChangedPostfix), "CustomRunLoad PlayerReadyChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "RemotePlayerDisconnected",
            typeof(CustomRunHooks), nameof(CustomRunLoadPlayerDisconnectedPostfix), "CustomRunLoad RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "LocalPlayerDisconnected",
            typeof(CustomRunHooks), nameof(CustomRunLoadLocalDisconnectedPostfix), "CustomRunLoad LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "BeginRun",
            typeof(CustomRunHooks), nameof(CustomRunLoadStateChangedPostfix), "CustomRunLoad BeginRun");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnEmbarkPressed",
            typeof(CustomRunHooks), nameof(CustomRunLoadEmbarkPostfix), "CustomRunLoad OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCustomRunLoadScreen), "OnUnreadyPressed",
            typeof(CustomRunHooks), nameof(CustomRunLoadUnreadyPostfix), "CustomRunLoad OnUnreadyPressed");
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
