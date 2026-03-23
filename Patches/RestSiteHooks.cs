using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class RestSiteHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_Ready",
            nameof(RestSiteReadyPostfix), "RestSite _Ready");
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_ExitTree",
            nameof(RestSiteExitPostfix), "RestSite _ExitTree");

        // Mend targeting — rest site characters are plain Controls, not NClickableControl
        PatchIfFound(harmony, typeof(NRestSiteCharacter), "OnFocus",
            nameof(RestSiteCharacterFocusPostfix), "RestSiteCharacter OnFocus");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(RestSiteHooks), handlerName, label, isPrefix);
    }

    public static void RestSiteReadyPostfix(NRestSiteRoom __instance)
    {
        if (RestSiteGameScreen.Current == null)
            ScreenManager.PushScreen(new RestSiteGameScreen(__instance));
    }

    public static void RestSiteExitPostfix()
    {
        if (RestSiteGameScreen.Current != null)
            ScreenManager.RemoveScreen(RestSiteGameScreen.Current);
    }

    public static void RestSiteCharacterFocusPostfix(NRestSiteCharacter __instance)
    {
        try
        {
            var player = __instance.Player;
            if (player == null) return;
            var name = player.Creature?.Name ?? MultiplayerHelper.GetPlayerName(player.NetId);
            var hp = $"{player.Creature.CurrentHp}/{player.Creature.MaxHp} HP";
            SpeechManager.Output($"{name}, {hp}");
        }
        catch (System.Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] RestSiteCharacter focus error: {e.Message}");
        }
    }
}
