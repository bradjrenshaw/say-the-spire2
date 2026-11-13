using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.UI;

/// <summary>
/// Ascension level titles and descriptions, for both game branches.
///
/// The November beta added game modes that renumber ascension levels (Blitz
/// has five levels mapped onto the standard ones), so
/// <c>AscensionHelper.GetTitle/GetDescription</c> gained a GameMode
/// parameter; stable still takes the level alone. A direct call would bind to
/// one signature at compile time and throw MissingMethodException on the
/// other branch, so both are resolved by name and invoked via reflection.
///
/// The game mode always comes from where the game itself reads it for the
/// same text: the on-screen ascension panel in lobbies (which the game keeps
/// in step with the lobby's mode for singleplayer, host and client, and with
/// the saved run on load screens), and the run state during a run.
/// </summary>
public static class AscensionText
{
    private static readonly MethodInfo TitleMethod =
        AccessTools.Method(typeof(AscensionHelper), "GetTitle")!;
    private static readonly MethodInfo DescriptionMethod =
        AccessTools.Method(typeof(AscensionHelper), "GetDescription")!;

    private static readonly bool TakesGameMode = TitleMethod.GetParameters().Length == 2;

    // Beta-only: stable's panel has no game mode (and its helper takes none).
    private static readonly FieldInfo? PanelGameModeField =
        AccessTools.Field(typeof(NAscensionPanel), "_gameMode");

    /// <summary>Title of a level as the given lobby ascension panel shows it.</summary>
    public static string Title(int level, NAscensionPanel panel) =>
        Format(TitleMethod, level, ModeOf(panel));

    /// <summary>Description of a level as the given lobby ascension panel shows it.</summary>
    public static string Description(int level, NAscensionPanel panel) =>
        Format(DescriptionMethod, level, ModeOf(panel));

    /// <summary>Title of a level in the given run.</summary>
    public static string Title(int level, RunState run) =>
        Format(TitleMethod, level, run.GameMode);

    /// <summary>Description of a level in the given run.</summary>
    public static string Description(int level, RunState run) =>
        Format(DescriptionMethod, level, run.GameMode);

    // Beta-only (and private in the game): the first line of the portrait
    // tip, "Game Mode: Dread" (", Multiplayer" appended in multiplayer).
    private static readonly MethodInfo? GameModeLineMethod =
        AccessTools.Method(typeof(AscensionHelper), "GetGameModeLine");

    /// <summary>
    /// The game mode line the portrait tip opens with on the November beta,
    /// or null on stable, whose tip has none.
    /// </summary>
    public static string? GameModeLine(RunState run) =>
        GameModeLineMethod?.Invoke(null, new object[] { run.GameMode, run.Players.Count > 1 }) is LocString line
            ? Localization.Message.StripBbcode(line.GetFormattedText())
            : null;

    // Whether a run earns no achievements or epochs. The check moved between
    // branches: the beta asks GameModeHelper about the run (daily and custom
    // runs), stable asks a GameMode extension (every non-standard mode).
    private static readonly MethodInfo? LockedForRunMethod =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Helpers.GameModeHelper") is { } helper
            ? AccessTools.Method(helper, "AreAchievementsAndEpochsLockedForRun")!
            : null;
    private static readonly MethodInfo? LockedForModeMethod =
        LockedForRunMethod != null
            ? null
            : AccessTools.Method(AccessTools.TypeByName("MegaCrit.Sts2.Core.Runs.GameModeExtension")!, "AreAchievementsAndEpochsLocked")!;

    /// <summary>
    /// The line the portrait tip ends with when the run earns no achievements
    /// or epochs ("Achievements &amp; Epochs are locked for this run."), or
    /// null when it does.
    /// </summary>
    public static string? AchievementsLockedLine(RunState run)
    {
        var locked = LockedForRunMethod != null
            ? LockedForRunMethod.Invoke(null, new object[] { run })
            : LockedForModeMethod!.Invoke(null, new object[] { run.GameMode });
        if (locked is not true) return null;

        var text = Localization.Message.StripBbcode(
            new LocString("gameplay_ui", "ACHIEVEMENTS_LOCKED").GetFormattedText()).Trim();
        return text.Length > 0 ? text : null;
    }

    private static object ModeOf(NAscensionPanel panel) =>
        PanelGameModeField?.GetValue(panel) ?? GameMode.Standard;

    private static string Format(MethodInfo method, int level, object gameMode)
    {
        var args = TakesGameMode ? new[] { level, gameMode } : new object[] { level };
        return method.Invoke(null, args) is LocString text ? text.GetFormattedText() : "";
    }
}
