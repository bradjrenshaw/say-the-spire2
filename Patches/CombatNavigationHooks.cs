using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Events;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

/// <summary>
/// Harmony registration for combat navigation fixes.
/// All behavior lives in CombatScreen.
/// </summary>
public static class CombatNavigationHooks
{
    public static void Initialize(Harmony harmony)
    {
        var updateNav = AccessTools.Method(typeof(NCombatRoom), "UpdateCreatureNavigation");
        if (updateNav != null)
        {
            harmony.Patch(updateNav,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(UpdateCreatureNavigationPrefix)),
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(UpdateCreatureNavigationPostfix)));
            Log.Info("[AccessibilityMod] Combat creature navigation hook patched.");
        }

        var startTargetingMethods = typeof(NTargetManager).GetMethods()
            .Where(m => m.Name == "StartTargeting");
        foreach (var method in startTargetingMethods)
        {
            harmony.Patch(method,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(StartTargetingPostfix)));
            Log.Info($"[AccessibilityMod] StartTargeting hook patched ({method.GetParameters().Length} params).");
        }

        var finishTargeting = AccessTools.Method(typeof(NTargetManager), "FinishTargeting");
        if (finishTargeting != null)
        {
            harmony.Patch(finishTargeting,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(FinishTargetingPostfix)));
            Log.Info("[AccessibilityMod] FinishTargeting hook patched.");
        }

        var takeTurn = AccessTools.Method(typeof(Creature), "TakeTurn");
        if (takeTurn != null)
        {
            harmony.Patch(takeTurn,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(TakeTurnPrefix)));
            Log.Info("[AccessibilityMod] Creature.TakeTurn hook patched.");
        }

        var shuffle = AccessTools.Method(typeof(CardPileCmd), "Shuffle");
        if (shuffle != null)
        {
            harmony.Patch(shuffle,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(ShufflePrefix)),
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(ShufflePostfix)));
            Log.Info("[AccessibilityMod] CardPileCmd.Shuffle hook patched.");
        }

        var refreshLayout = AccessTools.Method(typeof(NPlayerHand), "RefreshLayout");
        if (refreshLayout != null)
        {
            harmony.Patch(refreshLayout,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(RefreshLayoutPostfix)));
            Log.Info("[AccessibilityMod] Hand card navigation hook patched.");
        }
    }

    public static bool UpdateCreatureNavigationPrefix()
    {
        try
        {
            if (NTargetManager.Instance?.IsInSelection == true)
                return false;
        }
        catch { }
        return true;
    }

    public static void UpdateCreatureNavigationPostfix(NCombatRoom __instance)
        => CombatScreen.Current?.OnCreatureNavigationUpdated(__instance);

    public static void StartTargetingPostfix()
        => CombatScreen.Current?.OnTargetingStarted();

    public static void FinishTargetingPostfix()
        => CombatScreen.Current?.OnTargetingFinished();

    public static void RefreshLayoutPostfix(NPlayerHand __instance)
        => CombatScreen.Current?.OnHandLayoutRefreshed(__instance);

    public static void ShufflePrefix(Player player)
        => CombatScreen.Current?.OnShuffleStarted();

    public static void ShufflePostfix(Player player)
        => CombatScreen.Current?.OnShuffleFinished();

    public static void TakeTurnPrefix(Creature __instance)
    {
        try
        {
            if (__instance.IsMonster && __instance.Monster != null && !__instance.Monster.SpawnedThisTurn)
                EventDispatcher.Enqueue(new EnemyMoveEvent(__instance));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] TakeTurn prefix error: {e.Message}");
        }
    }
}
