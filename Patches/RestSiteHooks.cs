using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class RestSiteHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NRestSiteRoom), "_Ready",
            typeof(RestSiteHooks), nameof(RestSiteReadyPostfix), "RestSite _Ready");
        HarmonyHelper.PatchIfFound(harmony, typeof(NRestSiteRoom), "_ExitTree",
            typeof(RestSiteHooks), nameof(RestSiteExitPostfix), "RestSite _ExitTree");

        // Mend targeting — rest site characters are plain Controls, not NClickableControl
        HarmonyHelper.PatchIfFound(harmony, typeof(NRestSiteCharacter), "OnFocus",
            typeof(RestSiteHooks), nameof(RestSiteCharacterFocusPostfix), "RestSiteCharacter OnFocus");
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
            var name = MultiplayerHelper.GetPlayerDisplayName(player);
            var hp = Message.Localized("ui", "RESOURCE.HP", new { current = player.Creature.CurrentHp, max = player.Creature.MaxHp }).Resolve();
            SpeechManager.Output(Message.Raw($"{name}, {hp}"));
        }
        catch (System.Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] RestSiteCharacter focus error: {e.Message}");
        }
    }
}
