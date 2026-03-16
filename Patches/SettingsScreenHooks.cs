using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class SettingsScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuOpened",
            nameof(SettingsOpenedPostfix), "Settings OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuClosed",
            nameof(SettingsClosedPostfix), "Settings OnSubmenuClosed");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(SettingsScreenHooks), handlerName, label, isPrefix);
    }

    public static void SettingsOpenedPostfix(NSettingsScreen __instance)
    {
        if (SettingsGameScreen.Current == null)
        {
            var screen = new SettingsGameScreen(__instance);
            ScreenManager.PushScreen(screen);
        }
    }

    public static void SettingsClosedPostfix()
    {
        if (SettingsGameScreen.Current != null)
            ScreenManager.RemoveScreen(SettingsGameScreen.Current);
    }
}
