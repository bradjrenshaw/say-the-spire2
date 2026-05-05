using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class DailyLeaderboardScreen : Screen
{
    private readonly DailyLeaderboardAdapter _adapter;
    private readonly Control? _returnFocus;
    private readonly IReadOnlyList<UIElement> _extraActions;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "DAILY_RUN.LEADERBOARD"),
        AnnounceName = true,
        AnnouncePosition = false,
    };
    private readonly ListContainer _rows = new()
    {
        AnnounceName = false,
        AnnouncePosition = true,
    };
    private readonly ListContainer _extras = new()
    {
        AnnounceName = false,
        AnnouncePosition = false,
    };
    private readonly List<UIElement> _focusables = new();

    private string? _lastStateToken;
    private int _focusedIndex = -1;
    private bool _pendingDayFocus;
    private bool _pendingPageFocus;

    public override Message? ScreenName => Message.Localized("ui", "DAILY_RUN.LEADERBOARD");

    public DailyLeaderboardScreen(
        NDailyRunLeaderboard leaderboard,
        Control? returnFocus = null,
        IEnumerable<UIElement>? extraActions = null)
    {
        _adapter = new DailyLeaderboardAdapter(leaderboard);
        _returnFocus = returnFocus;
        _extraActions = extraActions?.ToList() ?? new List<UIElement>();
        _root.Add(_rows);
        _root.Add(_extras);
        RootElement = _root;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
    }

    public override void OnPush()
    {
        if (_adapter.CurrentPage != 0)
            _adapter.SetPage(0);

        Rebuild(forceFocusFirst: true);
    }

    public override void OnUpdate()
    {
        var token = _adapter.GetStateToken();
        if (token != _lastStateToken)
        {
            if (_pendingDayFocus && _adapter.IsLoading)
            {
                _lastStateToken = token;
            }
            else if (_pendingDayFocus)
            {
                Rebuild(forceFocusFirst: true);
                _pendingDayFocus = false;
            }
            else if (_pendingPageFocus && _adapter.IsLoading)
            {
                _lastStateToken = token;
            }
            else if (_pendingPageFocus)
            {
                Rebuild(forceFocusFirst: false, preferredIndex: GetFirstEntryIndex());
                _pendingPageFocus = false;
            }
            else
            {
                Rebuild(forceFocusFirst: false);
            }
        }

        EnsureFocus();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_up":
                return MoveRelative(-1);
            case "ui_down":
                return MoveRelative(1);
            case "ui_left":
                TryChangeDay(-1);
                return true;
            case "ui_right":
                TryChangeDay(1);
                return true;
            case "ui_accept":
            case "ui_select":
                return ActivateFocused();
            case "mega_view_deck_and_tab_left":
                if (_adapter.CanPagePrevious())
                {
                    _pendingPageFocus = true;
                    _adapter.ChangePage(-1);
                }
                return true;
            case "mega_view_exhaust_pile_and_tab_right":
                if (_adapter.CanPageNext())
                {
                    _pendingPageFocus = true;
                    _adapter.ChangePage(1);
                }
                return true;
            default:
                return false;
        }
    }

    private void Rebuild(bool forceFocusFirst, int? preferredIndex = null)
    {
        var previousIndex = _focusedIndex;

        _rows.Clear();
        _extras.Clear();
        _focusables.Clear();

        var dayLabel = _adapter.GetDayLabel();
        if (!string.IsNullOrWhiteSpace(dayLabel))
        {
            var dayElement = new ActionElement(
                () => Message.Raw(dayLabel),
                status: () => _adapter.GetEntries().Count == 0 ? _adapter.GetSummary() : null);
            _rows.Add(dayElement);
            _focusables.Add(dayElement);
        }

        var entries = _adapter.GetEntries();
        foreach (var entry in entries)
        {
            var element = new ActionElement(
                () => entry.Label,
                status: () => entry.Status);
            _rows.Add(element);
            _focusables.Add(element);
        }

        if (entries.Count == 0)
        {
            var fallback = new ActionElement(
                () => GetFallbackLabel(),
                status: () => _adapter.GetSummary());
            if (_focusables.Count == 0)
                _rows.Add(fallback);
            else
                _extras.Add(fallback);
            _focusables.Add(fallback);
        }

        AddExtraButton(
            label: Ui("DAILY_RUN_LEADERBOARD.PREVIOUS_PAGE"),
            enabled: _adapter.CanPagePrevious(),
            onActivated: () => _adapter.ChangePage(-1));
        AddExtraButton(
            label: Ui("DAILY_RUN_LEADERBOARD.NEXT_PAGE"),
            enabled: _adapter.CanPageNext(),
            onActivated: () => _adapter.ChangePage(1));

        if (_adapter.SupportsDayNavigation())
        {
            AddExtraButton(
                label: Ui("DAILY_RUN_LEADERBOARD.PREVIOUS_DAY"),
                enabled: _adapter.CanChangeDayPrevious(),
                onActivated: () => _adapter.ChangeDay(-1));
            AddExtraButton(
                label: Ui("DAILY_RUN_LEADERBOARD.NEXT_DAY"),
                enabled: _adapter.CanChangeDayNext(),
                onActivated: () => _adapter.ChangeDay(1));
        }

        if (_adapter.HasScoreWarning)
        {
            var warning = new ActionElement(
                () => Ui("DAILY_RUN_LEADERBOARD.SCORE_WARNING"),
                tooltip: () => Ui("DAILY_RUN_LEADERBOARD.SCORE_WARNING_TOOLTIP"));
            _extras.Add(warning);
            _focusables.Add(warning);
        }

        foreach (var action in _extraActions)
        {
            _extras.Add(action);
            _focusables.Add(action);
        }

        _lastStateToken = _adapter.GetStateToken();

        if (forceFocusFirst || previousIndex < 0)
        {
            FocusFirst();
            return;
        }

        if (preferredIndex.HasValue)
        {
            SetFocusIndex(Math.Clamp(preferredIndex.Value, 0, _focusables.Count - 1));
            return;
        }

        SetFocusIndex(Math.Min(previousIndex, _focusables.Count - 1));
    }

    public override void OnPop()
    {
        _returnFocus?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void AddExtraButton(Message label, bool enabled, Action onActivated)
    {
        var button = new ActionElement(
            () => label,
            status: () => enabled ? null : Ui("DAILY_RUN.DISABLED"),
            typeKey: () => "button",
            onActivated: enabled ? onActivated : null);
        _extras.Add(button);
        _focusables.Add(button);
    }

    private void FocusFirst()
    {
        if (_focusables.Count == 0)
            return;

        SetFocusIndex(0);
    }

    private int GetFirstEntryIndex()
    {
        var hasDayLabel = !string.IsNullOrWhiteSpace(_adapter.GetDayLabel());
        var hasEntries = _adapter.GetEntries().Count > 0;
        if (hasDayLabel && hasEntries)
            return 1;

        return 0;
    }

    private void EnsureFocus()
    {
        if (_focusedIndex < 0 || _focusedIndex >= _focusables.Count)
            FocusFirst();
    }

    private bool MoveRelative(int direction)
    {
        if (_focusables.Count == 0)
            return true;

        var target = _focusedIndex < 0
            ? 0
            : Math.Clamp(_focusedIndex + direction, 0, _focusables.Count - 1);

        if (target == _focusedIndex)
            return true;

        SetFocusIndex(target);
        return true;
    }

    private void SetFocusIndex(int index)
    {
        if (index < 0 || index >= _focusables.Count)
            return;

        if (_focusedIndex >= 0 && _focusedIndex < _focusables.Count)
            _focusables[_focusedIndex].Unfocus();

        _focusedIndex = index;
        UIManager.SetFocusedElement(_focusables[index]);
    }

    private bool ActivateFocused()
    {
        if (_focusedIndex < 0 || _focusedIndex >= _focusables.Count)
            return false;

        if (_focusables[_focusedIndex] is ActionElement action)
            return action.Activate();

        return false;
    }

    private void TryChangeDay(int delta)
    {
        if (!_adapter.SupportsDayNavigation())
            return;

        if (delta < 0)
        {
            if (_adapter.CanChangeDayPrevious())
            {
                _pendingDayFocus = true;
                _adapter.ChangeDay(-1);
            }
            return;
        }

        if (_adapter.CanChangeDayNext())
        {
            _pendingDayFocus = true;
            _adapter.ChangeDay(1);
        }
    }

    private Message GetFallbackLabel()
    {
        if (_adapter.IsLoading)
            return Ui("DAILY_RUN_LEADERBOARD.LOADING_SCORES");
        if (_adapter.HasNoScores)
            return Ui("DAILY_RUN_LEADERBOARD.NO_SCORES");
        if (_adapter.HasNoFriends)
            return Ui("DAILY_RUN_LEADERBOARD.NO_FRIENDS");

        return Ui("DAILY_RUN.LEADERBOARD");
    }

    private static Message Ui(string key) => Message.Localized("ui", key);
    private static string UiString(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
