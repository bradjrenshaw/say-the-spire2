using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class DailyRunHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "OnSubmenuOpened",
            typeof(DailyRunHooks), nameof(DailyRunOpenedPostfix), "DailyRun OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "OnSubmenuClosed",
            typeof(DailyRunHooks), nameof(DailyRunClosedPostfix), "DailyRun OnSubmenuClosed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "CleanUpLobby",
            typeof(DailyRunHooks), nameof(DailyRunClosedPostfix), "DailyRun CleanUpLobby");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "PlayerConnected",
            typeof(DailyRunHooks), nameof(DailyRunPlayerConnectedPostfix), "DailyRun PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "PlayerChanged",
            typeof(DailyRunHooks), nameof(DailyRunPlayerChangedPostfix), "DailyRun PlayerChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "RemotePlayerDisconnected",
            typeof(DailyRunHooks), nameof(DailyRunPlayerDisconnectedPostfix), "DailyRun RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "LocalPlayerDisconnected",
            typeof(DailyRunHooks), nameof(DailyRunLocalDisconnectedPostfix), "DailyRun LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "OnEmbarkPressed",
            typeof(DailyRunHooks), nameof(DailyRunEmbarkPostfix), "DailyRun OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunScreen), "OnUnreadyPressed",
            typeof(DailyRunHooks), nameof(DailyRunUnreadyPostfix), "DailyRun OnUnreadyPressed");

        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnSubmenuOpened",
            typeof(DailyRunHooks), nameof(DailyRunLoadOpenedPostfix), "DailyRunLoad OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnSubmenuClosed",
            typeof(DailyRunHooks), nameof(DailyRunLoadClosedPostfix), "DailyRunLoad OnSubmenuClosed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "CleanUpLobby",
            typeof(DailyRunHooks), nameof(DailyRunLoadClosedPostfix), "DailyRunLoad CleanUpLobby");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "PlayerConnected",
            typeof(DailyRunHooks), nameof(DailyRunLoadPlayerConnectedPostfix), "DailyRunLoad PlayerConnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "PlayerReadyChanged",
            typeof(DailyRunHooks), nameof(DailyRunLoadPlayerReadyChangedPostfix), "DailyRunLoad PlayerReadyChanged");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "RemotePlayerDisconnected",
            typeof(DailyRunHooks), nameof(DailyRunLoadPlayerDisconnectedPostfix), "DailyRunLoad RemotePlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "LocalPlayerDisconnected",
            typeof(DailyRunHooks), nameof(DailyRunLoadLocalDisconnectedPostfix), "DailyRunLoad LocalPlayerDisconnected");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnEmbarkPressed",
            typeof(DailyRunHooks), nameof(DailyRunLoadEmbarkPostfix), "DailyRunLoad OnEmbarkPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnUnreadyPressed",
            typeof(DailyRunHooks), nameof(DailyRunLoadUnreadyPostfix), "DailyRunLoad OnUnreadyPressed");
    }

    public static void DailyRunOpenedPostfix(NDailyRunScreen __instance)
    {
        if (DailyRunGameScreen.Current == null)
            ScreenManager.PushScreen(new DailyRunGameScreen(__instance));
    }

    public static void DailyRunClosedPostfix()
    {
        if (DailyRunGameScreen.Current != null)
            ScreenManager.RemoveScreen(DailyRunGameScreen.Current);
    }

    // Player params are object: the struct type is branch-divergent
    // (LobbyPlayer on stable, StartRunLobbyPlayer on beta), so Harmony boxes
    // it for us and LobbyPlayerView reads the shared field names.
    public static void DailyRunPlayerConnectedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            DailyRunGameScreen.Current?.OnPlayerConnected(view);
    }

    public static void DailyRunPlayerChangedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            DailyRunGameScreen.Current?.OnPlayerChanged(view);
    }

    public static void DailyRunPlayerDisconnectedPostfix(object player)
    {
        if (Views.LobbyPlayerView.FromBoxed(player) is { } view)
            DailyRunGameScreen.Current?.OnPlayerDisconnected(view);
    }

    public static void DailyRunLocalDisconnectedPostfix(NetErrorInfo info)
    {
        DailyRunGameScreen.Current?.OnLocalDisconnected();
    }

    public static void DailyRunEmbarkPostfix()
    {
        DailyRunGameScreen.Current?.OnEmbarkPressed();
    }

    public static void DailyRunUnreadyPostfix()
    {
        DailyRunGameScreen.Current?.OnUnreadyPressed();
    }

    public static void DailyRunLoadOpenedPostfix(NDailyRunLoadScreen __instance)
    {
        if (DailyRunLoadGameScreen.Current == null)
            ScreenManager.PushScreen(new DailyRunLoadGameScreen(__instance));
    }

    public static void DailyRunLoadClosedPostfix()
    {
        if (DailyRunLoadGameScreen.Current != null)
            ScreenManager.RemoveScreen(DailyRunLoadGameScreen.Current);
    }

    // __0 is object: the argument is `ulong playerId` on stable and
    // `LoadRunLobbyPlayer player` on the November beta, so it is matched by
    // position (the names differ) and boxed; LobbyPlayerView.IdOf reads either.
    public static void DailyRunLoadPlayerConnectedPostfix(object __0)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerConnected(Views.LobbyPlayerView.IdOf(__0));
    }

    public static void DailyRunLoadPlayerReadyChangedPostfix(ulong playerId)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerReadyChanged(playerId);
    }

    public static void DailyRunLoadPlayerDisconnectedPostfix(ulong playerId)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerDisconnected(playerId);
    }

    public static void DailyRunLoadLocalDisconnectedPostfix(NetErrorInfo info)
    {
        DailyRunLoadGameScreen.Current?.OnLocalDisconnected();
    }

    public static void DailyRunLoadEmbarkPostfix()
    {
        DailyRunLoadGameScreen.Current?.OnEmbarkPressed();
    }

    public static void DailyRunLoadUnreadyPostfix()
    {
        DailyRunLoadGameScreen.Current?.OnUnreadyPressed();
    }
}
