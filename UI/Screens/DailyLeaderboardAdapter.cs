using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

internal sealed class DailyLeaderboardAdapter
{
    private static readonly FieldInfo? ScoreContainerField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_scoreContainer");
    private static readonly FieldInfo? LoadingIndicatorField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_loadingIndicator");
    private static readonly FieldInfo? NoScoresIndicatorField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_noScoresIndicator");
    private static readonly FieldInfo? NoFriendsIndicatorField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_noFriendsIndicator");
    private static readonly FieldInfo? NoScoreUploadIndicatorField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_noScoreUploadIndicator");
    private static readonly FieldInfo? CurrentPageField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_currentPage");
    private static readonly FieldInfo? LeftArrowField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_leftArrow");
    private static readonly FieldInfo? RightArrowField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_rightArrow");
    private static readonly FieldInfo? PaginatorField =
        AccessTools.Field(typeof(NDailyRunLeaderboard), "_paginator");

    private static readonly MethodInfo? SetPageMethod =
        AccessTools.Method(typeof(NDailyRunLeaderboard), "SetPage");

    private static readonly FieldInfo? RowIsHeaderField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_isHeader");
    private static readonly FieldInfo? RowRankField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_rank");
    private static readonly FieldInfo? RowNameField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_name");
    private static readonly FieldInfo? RowScoreField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_score");
    private static readonly FieldInfo? RowFloorField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_floor");
    private static readonly FieldInfo? RowBadgesField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_badges");
    private static readonly FieldInfo? RowTimeField =
        AccessTools.Field(typeof(NDailyRunLeaderboardRow), "_time");

    private static readonly FieldInfo? PaginatorLabelField =
        AccessTools.Field(typeof(NLeaderboardDayPaginator), "_label");
    private static readonly FieldInfo? PaginatorLeftArrowField =
        AccessTools.Field(typeof(NLeaderboardDayPaginator), "_leftArrow");
    private static readonly FieldInfo? PaginatorRightArrowField =
        AccessTools.Field(typeof(NLeaderboardDayPaginator), "_rightArrow");
    private static readonly MethodInfo? PageLeftMethod =
        AccessTools.Method(typeof(NLeaderboardDayPaginator), "PageLeft");
    private static readonly MethodInfo? PageRightMethod =
        AccessTools.Method(typeof(NLeaderboardDayPaginator), "PageRight");

    private readonly NDailyRunLeaderboard _leaderboard;

    public DailyLeaderboardAdapter(NDailyRunLeaderboard leaderboard)
    {
        _leaderboard = leaderboard;
    }

    public readonly record struct Entry(string Label, string? Status);

    public int CurrentPage => (int?)CurrentPageField?.GetValue(_leaderboard) ?? 0;

    public bool IsLoading => IsVisible(LoadingIndicatorField);
    public bool HasNoScores => IsVisible(NoScoresIndicatorField);
    public bool HasNoFriends => IsVisible(NoFriendsIndicatorField);
    public bool HasScoreWarning => IsVisible(NoScoreUploadIndicatorField);

    public string? GetDayLabel()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        var label = PaginatorLabelField?.GetValue(paginator) as Label;
        return label == null ? null : GetControlText(label);
    }

    public string GetSummary()
    {
        if (IsLoading)
            return Ui("DAILY_RUN_LEADERBOARD.LOADING_PAGE", new { page = CurrentPage + 1 });
        if (HasNoScores)
            return Ui("DAILY_RUN_LEADERBOARD.PAGE_NO_SCORES", new { page = CurrentPage + 1 });
        if (HasNoFriends)
            return Ui("DAILY_RUN_LEADERBOARD.PAGE_NO_FRIENDS", new { page = CurrentPage + 1 });

        var entries = GetEntries();
        var parts = new List<string> { Ui("DAILY_RUN_LEADERBOARD.PAGE", new { page = CurrentPage + 1 }) };
        if (entries.Count > 0)
            parts.Add(Ui("DAILY_RUN_LEADERBOARD.ENTRIES", new { count = entries.Count }));
        var day = GetDayLabel();
        if (!string.IsNullOrWhiteSpace(day))
            parts.Add(day);
        return string.Join(", ", parts);
    }

    public IReadOnlyList<Entry> GetEntries()
    {
        var scoreContainer = ScoreContainerField?.GetValue(_leaderboard) as VBoxContainer;
        if (scoreContainer == null)
            return Array.Empty<Entry>();

        var results = new List<Entry>();
        foreach (var row in scoreContainer.GetChildren().OfType<NDailyRunLeaderboardRow>())
        {
            if ((bool?)RowIsHeaderField?.GetValue(row) == true)
                continue;

            var rank = GetControlText(RowRankField?.GetValue(row) as Control);
            var name = GetControlText(RowNameField?.GetValue(row) as Control);
            var score = GetControlText(RowScoreField?.GetValue(row) as Control);
            var floor = GetControlText(RowFloorField?.GetValue(row) as Control);
            var badges = GetControlText(RowBadgesField?.GetValue(row) as Control);
            var time = GetControlText(RowTimeField?.GetValue(row) as Control);

            string label;
            if (!string.IsNullOrWhiteSpace(name))
                label = string.IsNullOrWhiteSpace(rank) ? name : $"{rank}. {name}";
            else if (!string.IsNullOrWhiteSpace(rank))
                label = Ui("DAILY_RUN_LEADERBOARD.RANK_ONLY", new { rank });
            else
                label = Ui("DAILY_RUN_LEADERBOARD.ENTRY");

            var statusParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(score))
            {
                statusParts.Add(Ui("DAILY_RUN_LEADERBOARD.SCORE", new { score }));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(floor))
                    statusParts.Add(Ui("DAILY_RUN_LEADERBOARD.FLOOR", new { floor }));
                if (!string.IsNullOrWhiteSpace(badges))
                    statusParts.Add(Ui("DAILY_RUN_LEADERBOARD.BADGES", new { badges }));
                if (!string.IsNullOrWhiteSpace(time))
                    statusParts.Add(Ui("DAILY_RUN_LEADERBOARD.TIME", new { time }));
            }

            results.Add(new Entry(label, statusParts.Count > 0 ? string.Join(", ", statusParts) : null));
        }

        return results;
    }

    public IReadOnlyList<NDailyRunLeaderboardRow> GetRowControls()
    {
        var scoreContainer = ScoreContainerField?.GetValue(_leaderboard) as VBoxContainer;
        if (scoreContainer == null)
            return Array.Empty<NDailyRunLeaderboardRow>();

        return scoreContainer.GetChildren()
            .OfType<NDailyRunLeaderboardRow>()
            .Where(row => (bool?)RowIsHeaderField?.GetValue(row) != true)
            .ToList();
    }

    public bool CanPagePrevious()
    {
        return IsEnabled(LeftArrowField?.GetValue(_leaderboard) as NClickableControl);
    }

    public bool CanPageNext()
    {
        return IsEnabled(RightArrowField?.GetValue(_leaderboard) as NClickableControl);
    }

    public void ChangePage(int delta)
    {
        if (SetPageMethod == null)
            return;

        var target = Math.Max(0, CurrentPage + delta);
        SetPageMethod.Invoke(_leaderboard, new object[] { target });
    }

    public void SetPage(int page)
    {
        if (SetPageMethod == null)
            return;

        SetPageMethod.Invoke(_leaderboard, new object[] { Math.Max(0, page) });
    }

    public bool CanChangeDayPrevious()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        return IsEnabled(PaginatorLeftArrowField?.GetValue(paginator) as NClickableControl);
    }

    public bool CanChangeDayNext()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        return IsEnabled(PaginatorRightArrowField?.GetValue(paginator) as NClickableControl);
    }

    public bool SupportsDayNavigation()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        return IsVisible(PaginatorLeftArrowField?.GetValue(paginator) as Control)
            || IsVisible(PaginatorRightArrowField?.GetValue(paginator) as Control);
    }

    public NClickableControl? GetPreviousPageControl() => LeftArrowField?.GetValue(_leaderboard) as NClickableControl;
    public NClickableControl? GetNextPageControl() => RightArrowField?.GetValue(_leaderboard) as NClickableControl;
    public NClickableControl? GetPreviousDayControl()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        return PaginatorLeftArrowField?.GetValue(paginator) as NClickableControl;
    }

    public NClickableControl? GetNextDayControl()
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        return PaginatorRightArrowField?.GetValue(paginator) as NClickableControl;
    }

    public void ChangeDay(int delta)
    {
        var paginator = PaginatorField?.GetValue(_leaderboard) as NLeaderboardDayPaginator;
        if (paginator == null)
            return;

        if (delta < 0 && CanChangeDayPrevious())
            PageLeftMethod?.Invoke(paginator, null);
        else if (delta > 0 && CanChangeDayNext())
            PageRightMethod?.Invoke(paginator, null);
    }

    public string GetStateToken()
    {
        var entries = GetEntries();
        var first = entries.Count > 0 ? entries[0].Label : "";
        return string.Join("|",
            CurrentPage,
            GetDayLabel() ?? "",
            IsLoading,
            HasNoScores,
            HasNoFriends,
            HasScoreWarning,
            entries.Count,
            first);
    }

    private bool IsVisible(FieldInfo? field)
    {
        return (field?.GetValue(_leaderboard) as Control)?.Visible == true;
    }

    private static bool IsVisible(Control? control)
    {
        return control?.Visible == true;
    }

    private static bool IsEnabled(NClickableControl? control)
    {
        return control != null && control.Visible && control.IsEnabled;
    }

    private static string? GetControlText(Control? control)
    {
        return control switch
        {
            RichTextLabel rtl => ProxyElement.StripBbcode(rtl.Text).Trim(),
            Label label => label.Text.Trim(),
            null => null,
            _ => ProxyElement.FindChildTextPublic(control)?.Trim()
        };
    }

    private static string Ui(string key, object vars)
    {
        return Message.Localized("ui", key, vars).Resolve();
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
