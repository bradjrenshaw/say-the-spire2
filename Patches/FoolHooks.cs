using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Views;

namespace SayTheSpire2.Patches;

/// <summary>
/// Hooks for the Fool's combat panel (November beta; types resolved by name
/// and skipped on stable).
///
/// The game creates a melody symbol on the bow at the moment the local
/// player gains that melody, and only then — the panel belongs to your own
/// character. That creation is the announcement point.
/// </summary>
public static class FoolHooks
{
    public static void Initialize(Harmony harmony)
    {
        if (AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Combat.NFoolCombatSymbol") is { } symbolType)
            HarmonyHelper.PatchIfFound(harmony, symbolType, "Create",
                typeof(FoolHooks), nameof(MelodySymbolCreatedPostfix), "FoolCombatSymbol Create");
    }

    // __0 is the melody's CardTag, boxed: the Wah/Cho/Ki members are beta-only.
    public static void MelodySymbolCreatedPostfix(object __0)
    {
        try
        {
            var me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
            if (me?.Creature == null) return;
            EventDispatcher.Enqueue(new MelodyEvent(me.Creature, MelodiesView.TitleOf(__0)));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Melody gained hook error: {e.Message}");
        }
    }
}
