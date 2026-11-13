using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a Dread run (November beta game mode): ascension 10 with four
/// extra rules, a win streak in place of the ascension number on the top bar,
/// and a per-act "Dominion" — an extra rule named after the act's boss that
/// applies to every fight before it. The game shows the Dominion as the boss
/// icon's hover tip and the streak and rules in the portrait's.
///
/// <see cref="FromRunState"/> returns null for any other run and on stable,
/// where the mode doesn't exist; everything Dread-specific is resolved by
/// name. The mode is recognised by the name of the run's game mode value, as
/// the enum's members differ between branches (stable has no Dread, and its
/// value 3 is Custom).
/// </summary>
public sealed class DreadRunView
{
    // Beta-only; null on stable.
    private static readonly MethodInfo? DominionMethod =
        AccessTools.Method(typeof(EncounterModel), "GetDominion");

    // Resolved only where Dread exists; a rename there should crash.
    private static readonly MethodInfo? RunStreakMethod =
        DominionMethod == null ? null : AccessTools.Method(typeof(AscensionHelper), "GetDreadStreak", new[] { typeof(IRunState) })!;

    private readonly IRunState _run;

    private DreadRunView(IRunState run) { _run = run; }

    public static DreadRunView? FromRunState(IRunState? run) =>
        run != null && DominionMethod != null && run.GameMode.ToString() == "Dread"
            ? new DreadRunView(run)
            : null;

    /// <summary>The mode's name, as the mode select button names it.</summary>
    public string ModeTitle => new LocString("main_menu_ui", "DREAD.title").GetFormattedText();

    /// <summary>The local player's current win streak going into this run.</summary>
    public int Streak
    {
        get
        {
            try
            {
                return RunStreakMethod?.Invoke(null, new object[] { _run }) is int streak ? streak : 0;
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Dread run streak lookup failed: {e.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// "Streak 3", the game's own wording — null at zero, when the top bar
    /// shows no number.
    /// </summary>
    public string? StreakText
    {
        get
        {
            var streak = Streak;
            if (streak <= 0) return null;
            var header = new LocString("ascension", "PORTRAIT_DREAD_STREAK_HEADER");
            header.Add("streak", streak);
            return Message.StripBbcode(header.GetFormattedText());
        }
    }

    /// <summary>The four Dread rules, as the portrait tip lists them.</summary>
    public IReadOnlyList<string> Rules
    {
        get
        {
            var rules = new List<string>();
            for (int i = 1; i <= 4; i++)
                rules.Add(Message.StripBbcode(new LocString("ascension", $"DREAD_RULE_{i:D2}").GetFormattedText()));
            return rules;
        }
    }

    // The act's Dominion comes from its (last) boss, as the boss icon reads it.
    private EncounterModel DominionBoss => _run.Act.SecondBossEncounter ?? _run.Act.BossEncounter;

    private object? Dominion
    {
        get
        {
            try
            {
                return DominionMethod?.Invoke(DominionBoss, null);
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Dominion lookup failed: {e.Message}");
                return null;
            }
        }
    }

    public bool HasDominion => Dominion != null;

    /// <summary>"Test Subject's Dominion".</summary>
    public string DominionTitle
    {
        get
        {
            var title = new LocString("ascension", "DREAD.title");
            title.Add("BossName", DominionBoss.Title);
            return Message.StripBbcode(title.GetFormattedText());
        }
    }

    /// <summary>What the Dominion does ("All enemies gain 3 Strength.").</summary>
    public string? DominionDescription
    {
        get
        {
            var dominion = Dominion;
            if (dominion == null) return null;
            return AccessTools.Property(dominion.GetType(), "Description")!.GetValue(dominion) is LocString description
                ? Message.StripBbcode(description.GetFormattedText())
                : null;
        }
    }

    /// <summary>
    /// The Dominion's extra hover tips (cards or keywords it refers to), one
    /// line each.
    /// </summary>
    public IReadOnlyList<string> DominionExtraTips
    {
        get
        {
            var lines = new List<string>();
            var dominion = Dominion;
            if (dominion == null) return lines;

            if (AccessTools.Property(dominion.GetType(), "ExtraHoverTips")!.GetValue(dominion) is not IEnumerable tips)
                return lines;

            foreach (var tip in tips)
            {
                if (tip is HoverTip hoverTip)
                {
                    var description = Message.StripBbcode(hoverTip.Description ?? "");
                    var line = !string.IsNullOrEmpty(hoverTip.Title) && description.Length > 0
                        ? $"{hoverTip.Title}: {description}"
                        : !string.IsNullOrEmpty(hoverTip.Title) ? hoverTip.Title : description;
                    if (!string.IsNullOrEmpty(line))
                        lines.Add(line);
                }
                else if (tip is CardHoverTip cardTip && cardTip.Card != null)
                {
                    var formatted = CardBuffer.FormatHoverTip(cardTip.Card);
                    if (!string.IsNullOrEmpty(formatted))
                        lines.Add(formatted);
                }
            }
            return lines;
        }
    }
}
