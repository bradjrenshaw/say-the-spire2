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

    private static object ModeOf(NAscensionPanel panel) =>
        PanelGameModeField?.GetValue(panel) ?? GameMode.Standard;

    private static string Format(MethodInfo method, int level, object gameMode)
    {
        var args = TakesGameMode ? new[] { level, gameMode } : new object[] { level };
        return method.Invoke(null, args) is LocString text ? text.GetFormattedText() : "";
    }
}
