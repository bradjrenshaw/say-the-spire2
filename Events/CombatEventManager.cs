using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Events;

public static class CombatEventManager
{
    private static CombatScreen? _activeCombatScreen;

    public static void Initialize()
    {
        var cm = CombatManager.Instance;
        cm.CombatSetUp += OnCombatSetUp;
        cm.CombatEnded += OnCombatEnded;
        Log.Info("[AccessibilityMod] CombatEventManager initialized.");
    }

    private static void OnCombatSetUp(CombatState state)
    {
        _activeCombatScreen = new CombatScreen(state);
        ScreenManager.PushScreen(_activeCombatScreen);
    }

    private static void OnCombatEnded(CombatRoom _)
    {
        CleanUp();
    }

    /// <summary>
    /// Remove the active combat screen if one exists. Called both when combat
    /// ends normally and when the run ends (combat may not end cleanly on death/abandon).
    /// </summary>
    public static void CleanUp()
    {
        if (_activeCombatScreen != null)
        {
            ScreenManager.RemoveScreen(_activeCombatScreen);
            _activeCombatScreen = null;
        }
    }
}
