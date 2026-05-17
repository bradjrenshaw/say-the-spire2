using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SayTheSpire2.Events;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;
using SayTheSpire2.Views;

namespace SayTheSpire2.Patches;

/// <summary>
/// Harmony patches for combat events that can't be handled frame-driven.
/// Focus neighbor management is now done in CombatScreen.UpdateFocusNavigation().
/// </summary>
public static class CombatNavigationHooks
{
    public static void Initialize(Harmony harmony)
    {
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
    }

    public static void StartTargetingPostfix()
        => CombatScreen.Current?.OnTargetingStarted();

    public static void FinishTargetingPostfix()
        => CombatScreen.Current?.OnTargetingFinished();

    public static void TakeTurnPrefix(Creature __instance)
    {
        try
        {
            if (__instance.IsMonster && __instance.Monster != null && !__instance.Monster.SpawnedThisTurn)
            {
                var intentSummary = CreatureIntentFormatter.Summary(CreatureView.FromEntity(__instance), includePrefix: false);
                EventDispatcher.Enqueue(new EnemyMoveEvent(__instance, intentSummary));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] TakeTurn prefix error: {e.Message}");
        }
    }

    // The game's CardPileCmd.Shuffle runs for every player in a multiplayer
    // session; visually only the local player's draw pile becoming empty is
    // surfaced to the user, so we filter remote-player shuffles out before
    // forwarding to CombatScreen.
    public static void ShufflePrefix(Player player)
    {
        if (!MultiplayerHelper.IsLocalPlayer(player)) return;
        CombatScreen.Current?.OnShuffleStarting();
    }

    public static void ShufflePostfix(Player player, Task __result)
    {
        if (!MultiplayerHelper.IsLocalPlayer(player)) return;
        CombatScreen.Current?.OnShuffleStarted(__result);
    }
}
