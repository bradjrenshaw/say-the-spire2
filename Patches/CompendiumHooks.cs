using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class CompendiumHooks
{
    private static Screen? _currentScreen;
    private static NSubmenu? _currentSubmenu;

    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(
            harmony,
            typeof(NSubmenuStack),
            "Push",
            typeof(CompendiumHooks),
            nameof(PushPostfix),
            "Compendium stack Push");
        HarmonyHelper.PatchIfFound(
            harmony,
            typeof(NSubmenuStack),
            "Pop",
            typeof(CompendiumHooks),
            nameof(PopPostfix),
            "Compendium stack Pop");
    }

    public static void PushPostfix(NSubmenuStack __instance, NSubmenu screen)
    {
        try
        {
            if (!IsSupportedStack(__instance))
                return;

            SyncTo(screen);
        }
        catch (System.Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Compendium PushPostfix error: {e}");
        }
    }

    public static void PopPostfix(NSubmenuStack __instance)
    {
        try
        {
            if (!IsSupportedStack(__instance))
                return;

            SyncTo(__instance.Peek());
        }
        catch (System.Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Compendium PopPostfix error: {e}");
        }
    }

    private static void SyncTo(NSubmenu? submenu)
    {
        if (ReferenceEquals(_currentSubmenu, submenu) && _currentScreen != null)
            return;

        if (_currentScreen != null)
        {
            ScreenManager.RemoveFromTree(_currentScreen);
            _currentScreen = null;
            _currentSubmenu = null;
        }

        var next = CreateScreen(submenu);
        if (next == null)
            return;

        _currentSubmenu = submenu;
        _currentScreen = next;
        ScreenManager.PushScreen(next);
    }

    private static Screen? CreateScreen(NSubmenu? submenu)
    {
        return submenu switch
        {
            NCompendiumSubmenu compendium => new CompendiumMenuScreen(compendium),
            NPotionLab potionLab => new PotionLabGameScreen(potionLab),
            NRelicCollection relicCollection => new RelicCollectionGameScreen(relicCollection),
            NStatsScreen statsScreen => new StatsGameScreen(statsScreen),
            NCardLibrary cardLibrary => new CardLibraryGameScreen(cardLibrary),
            NRunHistory runHistory => new RunHistoryGameScreen(runHistory),
            NBestiary bestiary => new BestiaryGameScreen(bestiary),
            NJoinFriendScreen joinFriend => new JoinFriendScreen(joinFriend),
            _ => null,
        };
    }

    private static bool IsSupportedStack(NSubmenuStack stack)
    {
        return stack is NRunSubmenuStack or NMainMenuSubmenuStack;
    }
}
