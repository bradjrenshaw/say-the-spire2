using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class SettingsScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuOpened",
            typeof(SettingsScreenHooks), nameof(SettingsOpenedPostfix), "Settings OnSubmenuOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuClosed",
            typeof(SettingsScreenHooks), nameof(SettingsClosedPostfix), "Settings OnSubmenuClosed");
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
