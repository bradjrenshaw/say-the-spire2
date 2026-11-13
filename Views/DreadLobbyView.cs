using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// View over the Dread lobby (<c>NDreadScreen</c>, November beta). It has the
/// same character buttons as character select but no info panel and no
/// ascension control (Dread is fixed at ascension 10). What it shows instead:
/// <list type="bullet">
/// <item>the selected character's win streak, as a drawn chain with one link
/// per win (no number anywhere);</item>
/// <item>their best streaks, as text in the corner;</item>
/// <item>a "Begin Rotating Streak" tickbox (singleplayer, while no rotating
/// streak is running), and once one is running a "Rotating" button whose
/// name reads "Next: {character}".</item>
/// </list>
/// Everything is resolved by name: the screen doesn't exist on stable, where
/// <see cref="FromScreen"/> and <see cref="FromControl"/> return null.
/// </summary>
public sealed class DreadLobbyView
{
    public const string ScreenTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen";

    // Beta-only; null on stable.
    private static readonly Type? ScreenType = AccessTools.TypeByName(ScreenTypeName);

    // Resolved only where the screen exists; a rename there should crash.
    private static readonly PropertyInfo? LobbyProperty =
        ScreenType == null ? null : AccessTools.Property(ScreenType, "Lobby")!;
    private static readonly FieldInfo? RotatingOverrideField =
        ScreenType == null ? null : AccessTools.Field(ScreenType, "_rotatingDisplayCharacterOverride")!;
    private static readonly FieldInfo? RotatingCharacterField =
        ScreenType == null ? null : AccessTools.Field(ScreenType, "_rotatingCharacter")!;
    private static readonly MethodInfo? StreakMethod =
        ScreenType == null ? null : AccessTools.Method(typeof(AscensionHelper), "GetDreadStreak",
            new[] { typeof(CharacterModel), typeof(bool), typeof(bool) })!;
    private static readonly PropertyInfo? ShortTitleProperty =
        ScreenType == null ? null : AccessTools.Property(typeof(CharacterModel), "ShortTitle")!;

    private readonly Control _screen;

    private DreadLobbyView(Control screen) { _screen = screen; }

    public static DreadLobbyView? FromScreen(Control? screen) =>
        screen != null && ScreenType != null && ScreenType.IsInstanceOfType(screen)
            ? new DreadLobbyView(screen)
            : null;

    /// <summary>The Dread lobby a control (a character button) sits in, if any.</summary>
    public static DreadLobbyView? FromControl(Node? control)
    {
        if (ScreenType == null) return null;
        for (var node = control; node != null; node = node.GetParent())
        {
            if (node is Control screen && ScreenType.IsInstanceOfType(screen))
                return new DreadLobbyView(screen);
        }
        return null;
    }

    public bool IsMultiplayer =>
        (LobbyProperty?.GetValue(_screen) as StartRunLobby)?.NetService.Type.IsMultiplayer() == true;

    /// <summary>The "Begin Rotating Streak" tickbox.</summary>
    public NTickbox? Tickbox => _screen.GetNodeOrNull<NTickbox>("%RotatingTickbox");

    /// <summary>
    /// The game hides the tickbox in multiplayer, on the Rotating button and
    /// while a rotating streak is running, and disables it during the streak
    /// animation and after embarking.
    /// </summary>
    public bool IsTickboxUsable => Tickbox is { } tickbox && tickbox.IsVisibleInTree() && tickbox.IsEnabled;

    /// <summary>
    /// The tickbox's caption, as shown ("Begin Rotating Streak").
    /// </summary>
    public string TickboxLabel
    {
        get
        {
            var shown = _screen.GetNodeOrNull<RichTextLabel>("%RotatingExplanation")?.Text;
            return !string.IsNullOrWhiteSpace(shown)
                ? Message.StripBbcode(shown).Trim()
                : new LocString("main_menu_ui", "DREAD_ROTATION_TICKBOX").GetFormattedText();
        }
    }

    /// <summary>The hover tip the tickbox shows: what a rotating streak is.</summary>
    public static (string Title, string Description) TickboxTip => (
        new LocString("static_hover_tips", "DREAD_ROTATION.title").GetFormattedText(),
        Message.StripBbcode(new LocString("static_hover_tips", "DREAD_ROTATION.description").GetFormattedText()));

    /// <summary>
    /// True for the placeholder character on the lobby's Rotating button
    /// (beta-only type; the game also flags that button as random).
    /// </summary>
    public static bool IsRotating(CharacterModel? character) =>
        character?.GetType().Name == "RotatingCharacter";

    // Embarking with the tickbox ticked starts a rotating streak, so the game
    // then shows the rotating streak and records, not the character's own.
    private bool IsBeginningRotation =>
        !IsMultiplayer && Tickbox is { } tickbox && tickbox.IsVisibleInTree() && tickbox.IsTicked;

    /// <summary>
    /// "Next: Silent" — whose turn it is on the running rotating streak, as
    /// the name banner reads when the Rotating button is selected.
    /// </summary>
    public string? RotatingNextText
    {
        get
        {
            var next = (RotatingOverrideField?.GetValue(_screen) ?? RotatingCharacterField?.GetValue(_screen)) as CharacterModel;
            if (next == null || ShortTitleProperty?.GetValue(next) is not LocString name)
                return null;
            var text = new LocString("main_menu_ui", "DREAD_ROTATION_CHARACTER");
            text.Add("CharacterName", name);
            return Message.StripBbcode(text.GetFormattedText());
        }
    }

    /// <summary>
    /// The streak information the lobby shows for a character button, one
    /// entry per line: the current streak ("Streak 3", the chain — omitted at
    /// zero, when the game draws no links) and the best-streak records
    /// ("Best Streak 5"; the game leaves those out until there is one).
    ///
    /// The records are read off the screen, where they describe the selected
    /// character — focusing a button selects it, so by the time it is
    /// announced they are that button's.
    /// </summary>
    public IReadOnlyList<string> StreakLinesFor(NCharacterSelectButton button)
    {
        var lines = new List<string>();
        var character = button.Character;
        if (character == null || button.IsLocked)
            return lines;

        try
        {
            var rotating = IsRotating(character) || IsBeginningRotation;
            if (StreakMethod?.Invoke(null, new object[] { character, IsMultiplayer, rotating }) is int streak && streak > 0)
            {
                var header = new LocString("ascension", "PORTRAIT_DREAD_STREAK_HEADER");
                header.Add("streak", streak);
                lines.Add(Message.StripBbcode(header.GetFormattedText()));
            }
        }
        catch (Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] Dread streak lookup failed: {e.Message}");
        }

        if (button.IsSelected)
            lines.AddRange(RecordLines());
        return lines;
    }

    private IEnumerable<string> RecordLines()
    {
        var labels = SplitLines(_screen.GetNodeOrNull<RichTextLabel>("%ScoreLabels")?.Text);
        var values = SplitLines(_screen.GetNodeOrNull<RichTextLabel>("%ScoreValues")?.Text);
        for (int i = 0; i < labels.Length && i < values.Length; i++)
        {
            // The game never shows a zero record; a 0 here is the scene's
            // placeholder text from before the first selection.
            if (labels[i].Length == 0 || values[i].Length == 0 || values[i] == "0")
                continue;
            yield return $"{labels[i]} {values[i]}";
        }
    }

    private static string[] SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : Array.ConvertAll(Message.StripBbcode(text).Split('\n'), line => line.Trim());
}
