using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class GameOverScreen : GameScreen
{
    public static GameOverScreen? Current { get; private set; }

    public override Message? ScreenName => null; // Announced via banner instead

    private readonly NGameOverScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "CONTAINERS.GAME_OVER"),
        AnnounceName = false,
        AnnouncePosition = false,
    };
    private readonly RowContainer _victory = new() { AnnounceName = false, AnnouncePosition = false };
    private readonly RowContainer _scores = NewRow("GAME_OVER.ROWS.SCORES");
    private readonly RowContainer _badges = NewRow("GAME_OVER.ROWS.BADGES", announcePosition: true);
    private readonly RowContainer _discoveries = NewRow("GAME_OVER.ROWS.DISCOVERIES");
    private readonly Dictionary<ListContainer, List<Control>> _rowControls = new();
    private readonly List<Control> _mainControls = new();

    private static readonly FieldInfo? ScoreField =
        AccessTools.Field(typeof(NGameOverScreen), "_score");

    private static readonly FieldInfo? ScoreUnlockedEpochIdField =
        AccessTools.Field(typeof(NGameOverScreen), "_scoreUnlockedEpochId");

    private static readonly FieldInfo? EncounterQuoteField =
        AccessTools.Field(typeof(NGameOverScreen), "_encounterQuote");

    private static readonly FieldInfo? HistoryField =
        AccessTools.Field(typeof(NGameOverScreen), "_history");

    private static readonly FieldInfo? BadgeTitleField =
        AccessTools.Field(typeof(NBadge), "_title");

    private static readonly FieldInfo? BadgeDescriptionField =
        AccessTools.Field(typeof(NBadge), "_description");

    private static readonly FieldInfo? DiscoveredItemHoverTipField =
        AccessTools.Field(typeof(NDiscoveredItem), "_hoverTip");

    private ulong? _summaryInstanceId;
    private bool _summarySettled;
    private NDailyRunLeaderboard? _leaderboard;

    public GameOverScreen(NGameOverScreen screen)
    {
        _screen = screen;
        RootElement = _root;
    }

    protected override void BuildRegistry()
    {
        ClearRegistry();
        _root.Clear();
        _victory.Clear();
        _scores.Clear();
        _badges.Clear();
        _discoveries.Clear();
        _rowControls.Clear();
        _mainControls.Clear();

        if (_summarySettled)
            RegisterSummaryRows();
        else
            RegisterInitialControls();

        AddRowIfNotEmpty(_victory);
        AddRowIfNotEmpty(_scores);
        AddRowIfNotEmpty(_badges);
        AddRowIfNotEmpty(_discoveries);
        WireFocusOrder();
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen))
        {
            ScreenManager.RemoveScreen(this);
            return;
        }

        if (_leaderboard != null && !GodotObject.IsInstanceValid(_leaderboard))
            _leaderboard = null;

        _leaderboard ??= _screen.GetNodeOrNull<NDailyRunLeaderboard>("%DailyRunLeaderboard");
        if (_leaderboard != null
            && GodotObject.IsInstanceValid(_leaderboard)
            && _leaderboard.Visible
            && ActiveChild is not DailyLeaderboardScreen)
            PushGameOverLeaderboard();
    }

    public void OnBannerAndQuote(NGameOverScreen instance)
    {
        try
        {
            var banner = instance.GetNodeOrNull("%Banner");
            var quoteLabel = instance.GetNodeOrNull("%DeathQuoteLabel");

            string? title = null;
            if (banner != null)
                title = ProxyElement.FindChildTextPublic(banner);

            // Read the encounter-specific quote (e.g., "The Ironclad had simply given up.")
            // This is set during InitializeBannerAndQuote but only displayed later via animation.
            string? quote = EncounterQuoteField?.GetValue(instance) as string;
            if (string.IsNullOrEmpty(quote) && quoteLabel is RichTextLabel rtl)
                quote = Message.StripBbcode(rtl.Text);

            var message = "";
            if (!string.IsNullOrEmpty(title))
                message = title;
            if (!string.IsNullOrEmpty(quote))
                message += string.IsNullOrEmpty(message) ? quote : $". {quote}";

            if (!string.IsNullOrEmpty(message))
            {
                var output = Message.Raw(message);
                Log.Info($"[AccessibilityMod] Game over: {message}");
                SpeechManager.Output(output);

                var uiBuffer = BufferManager.Instance.GetBuffer("ui");
                if (uiBuffer != null)
                {
                    uiBuffer.Clear();
                    uiBuffer.Add(message);
                    BufferManager.Instance.EnableBuffer("ui", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver banner error: {ex.Message}");
        }
    }

    public void OnSummaryAnimationStarted(NGameOverScreen instance)
    {
        _summaryInstanceId = instance.GetInstanceId();
        _summarySettled = false;
        Log.Info("[AccessibilityMod] GameOver summary animation started.");
    }

    public void OnSummarySettled(NGameOverScreen instance)
    {
        try
        {
            var instanceId = instance.GetInstanceId();
            if (_summarySettled && _summaryInstanceId == instanceId)
                return;

            _summaryInstanceId = instanceId;
            _summarySettled = true;

            BuildRegistry();
            FocusFirstSummaryItem();
            Log.Info($"[AccessibilityMod] GameOver summary settled ({GetRegisteredControls().Count()} controls registered).");
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver summary settled error: {ex.Message}");
        }
    }

    private void RegisterInitialControls()
    {
        RegisterMainButton("%ContinueButton");
    }

    private void RegisterSummaryRows()
    {
        RegisterVictorySummary();
        RegisterScoreLines();
        RegisterBadges();
        RegisterDiscoveries();
        RegisterMainButton("%ViewRunButton");
        RegisterMainButton("%LeaderboardButton");
        RegisterMainButton("%MainMenuButton");
    }

    private void RegisterVictorySummary()
    {
        if (!IsRunWin())
            return;

        var label = _screen.GetNodeOrNull<Control>("%VictoryDamageLabel");
        var focusTarget = _screen.GetNodeOrNull<Control>("%RunSummaryContainer");
        if (!IsUsable(focusTarget) || label == null) return;

        var text = GetNodeText(label);
        if (string.IsNullOrEmpty(text)) return;

        // The game appends ascension-unlock text to this label only when it
        // actually grants that unlock, so do not reconstruct that condition here.
        RegisterFocusable(focusTarget!, _victory, new ActionElement(() => Message.Raw(text)));
    }

    private void RegisterScoreLines()
    {
        RegisterTotalScore();

        var container = _screen.GetNodeOrNull<Control>("%ScoreLineContainer");
        if (container == null) return;

        foreach (var line in container.GetChildren().OfType<Control>().Where(IsUsable))
        {
            var label = GetNodeText(line);
            if (string.IsNullOrEmpty(label)) continue;
            RegisterFocusable(line, _scores, new ActionElement(() => Message.Raw(label)));
        }

        RegisterTextNode("%UnlocksRemaining", _scores, requireVisibleText: true);
        RegisterTextNode("%ScoreProgress", _scores, requireVisibleText: true);
        if (HasUnlockedEpoch())
            RegisterTextNode("%UnlockText", _scores, requireVisibleText: true);
    }

    private void RegisterTotalScore()
    {
        var scoreControl = _screen.GetNodeOrNull<Control>("%ScoreBar")
            ?? _screen.GetNodeOrNull<Control>("%ScoreLineContainer");
        if (scoreControl == null) return;

        var score = ScoreField?.GetValue(_screen);
        if (score is not int scoreVal) return;

        RegisterFocusable(scoreControl, _scores,
            new ActionElement(() => Message.Localized("ui", "GAME_OVER.SCORE", new { score = scoreVal })));
    }

    private void RegisterBadges()
    {
        var container = _screen.GetNodeOrNull<Control>("%BadgeContainer");
        if (container == null) return;

        foreach (var badge in container.GetChildren().OfType<NBadge>().Where(IsUsable))
        {
            var title = (BadgeTitleField?.GetValue(badge) as LocString)?.GetFormattedText();
            var description = (BadgeDescriptionField?.GetValue(badge) as LocString)?.GetFormattedText();
            if (string.IsNullOrWhiteSpace(title)) continue;

            RegisterFocusable(badge, _badges, new ActionElement(
                () => Message.Raw(Message.StripBbcode(title)),
                tooltip: string.IsNullOrWhiteSpace(description)
                    ? null
                    : () => Message.Raw(Message.StripBbcode(description))));
        }
    }

    private void RegisterDiscoveries()
    {
        var summary = _screen.GetNodeOrNull<Control>("%RunSummaryContainer");
        if (summary == null) return;

        foreach (var item in summary.FindChildren("*", nameof(NDiscoveredItem), recursive: true, owned: false).OfType<NDiscoveredItem>())
        {
            if (!IsUsable(item)) continue;
            if (DiscoveredItemHoverTipField?.GetValue(item) is not HoverTip hoverTip) continue;
            if (string.IsNullOrWhiteSpace(hoverTip.Title)) continue;

            RegisterFocusable(item, _discoveries, new ActionElement(
                () => Message.Raw(Message.StripBbcode(hoverTip.Title)),
                status: () => Message.Raw(GetNodeText(item) ?? ""),
                tooltip: string.IsNullOrWhiteSpace(hoverTip.Description)
                    ? null
                    : () => Message.Raw(Message.StripBbcode(hoverTip.Description))));
        }
    }

    private void RegisterTextNode(string path, ListContainer row, bool requireVisibleText = false)
    {
        var control = _screen.GetNodeOrNull<Control>(path);
        if (!IsUsable(control)) return;
        if (requireVisibleText && !IsTextVisible(control!)) return;

        var label = GetNodeText(control!);
        if (string.IsNullOrEmpty(label)) return;
        RegisterFocusable(control!, row, new ActionElement(() => Message.Raw(label)));
    }

    private void RegisterButton(string path, ListContainer row)
    {
        var control = _screen.GetNodeOrNull<NClickableControl>(path);
        if (!IsUsable(control)) return;

        RegisterFocusable(control!, row, new ProxyButton(control!));
    }

    private void RegisterMainButton(string path)
    {
        var control = _screen.GetNodeOrNull<NClickableControl>(path);
        if (!IsUsable(control)) return;

        var element = new ProxyButton(control!);
        control!.FocusMode = Control.FocusModeEnum.All;
        _root.Add(element);
        Register(control, element);
        ConnectFocusSignal(control, element);
        _mainControls.Add(control);
    }

    private void PushGameOverLeaderboard()
    {
        if (_leaderboard == null)
            return;

        var exitButton = _screen.GetNodeOrNull<NClickableControl>("%MainMenuButton");
        var exitAction = CreateButtonAction(exitButton);
        var extraActions = exitAction == null ? null : new[] { exitAction };
        PushChild(new DailyLeaderboardScreen(_leaderboard, (Control?)exitButton ?? _leaderboard, extraActions));
    }

    private static ActionElement? CreateButtonAction(NClickableControl? button)
    {
        if (!IsUsable(button))
            return null;

        var proxy = new ProxyButton(button!);
        return new ActionElement(
            () => proxy.GetLabel(),
            status: () => proxy.GetStatusString(),
            typeKey: () => "button",
            onActivated: () => Activate(button));
    }

    private void RegisterFocusable(Control control, ListContainer row, UIElement element)
    {
        control.FocusMode = Control.FocusModeEnum.All;
        row.Add(element);
        Register(control, element);
        ConnectFocusSignal(control, element);
        if (!_rowControls.TryGetValue(row, out var controls))
        {
            controls = new List<Control>();
            _rowControls[row] = controls;
        }
        controls.Add(control);
    }

    private void AddRowIfNotEmpty(ListContainer row)
    {
        if (row.Children.Any())
            _root.Add(row);
    }

    private void WireFocusOrder()
    {
        var sections = _root.Children.OfType<ListContainer>()
            .Where(row => _rowControls.TryGetValue(row, out var controls) && controls.Count > 0)
            .Select(row => _rowControls[row])
            .Concat(_mainControls.Select(control => new List<Control> { control }))
            .ToList();
        if (sections.Count == 0)
            return;

        var first = sections[0][0];
        if (GodotObject.IsInstanceValid(first))
        {
            _screen.FocusNeighborTop = first.GetPath();
            _screen.FocusNeighborBottom = first.GetPath();
            _screen.FocusNeighborLeft = first.GetPath();
            _screen.FocusNeighborRight = first.GetPath();
        }

        for (var rowIndex = 0; rowIndex < sections.Count; rowIndex++)
        {
            var controls = sections[rowIndex];
            var previousRow = rowIndex > 0 ? sections[rowIndex - 1] : null;
            var nextRow = rowIndex + 1 < sections.Count ? sections[rowIndex + 1] : null;

            for (var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                if (!GodotObject.IsInstanceValid(control)) continue;

                var left = i > 0 ? controls[i - 1] : control;
                var right = i + 1 < controls.Count ? controls[i + 1] : control;
                var up = previousRow == null ? control : previousRow[System.Math.Min(i, previousRow.Count - 1)];
                var down = nextRow == null ? control : nextRow[System.Math.Min(i, nextRow.Count - 1)];

                control.FocusNeighborLeft = left.GetPath();
                control.FocusNeighborRight = right.GetPath();
                control.FocusNeighborTop = up.GetPath();
                control.FocusNeighborBottom = down.GetPath();
            }
        }
    }

    private void FocusFirstSummaryItem()
    {
        var target = GetFirstSummaryFocusTarget();
        if (target == null)
            return;

        target.CallDeferred(Control.MethodName.GrabFocus);
    }

    private Control? GetFirstSummaryFocusTarget()
    {
        if (TryGetFirstRowControl(_victory, out var victory))
            return victory;
        if (TryGetFirstRowControl(_scores, out var score))
            return score;

        return null;
    }

    private bool TryGetFirstRowControl(ListContainer row, out Control? control)
    {
        control = null;
        if (!_rowControls.TryGetValue(row, out var controls))
            return false;

        control = controls.FirstOrDefault(IsUsable);
        return control != null;
    }

    private static RowContainer NewRow(string key, bool announcePosition = false) => new()
    {
        ContainerLabel = Message.Localized("ui", key),
        AnnounceName = true,
        AnnouncePosition = announcePosition,
    };

    private bool HasUnlockedEpoch()
    {
        try
        {
            return !string.IsNullOrEmpty(ScoreUnlockedEpochIdField?.GetValue(_screen) as string);
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver unlock state error: {ex.Message}");
            return false;
        }
    }

    private bool IsRunWin()
    {
        try
        {
            return HistoryField?.GetValue(_screen) is RunHistory { Win: true };
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver history state error: {ex.Message}");
            return false;
        }
    }

    private static string? GetNodeText(Node node)
    {
        var parts = new List<string>();
        CollectText(node, parts);
        return NormalizeText(string.Join(", ", parts));
    }

    private static bool IsTextVisible(Control control)
    {
        if (control.Modulate.A <= 0.05f || control.SelfModulate.A <= 0.05f)
            return false;

        return HasVisibleText(control);
    }

    private static bool HasVisibleText(Node node)
    {
        if (node is CanvasItem canvasItem && (canvasItem.Modulate.A <= 0.05f || canvasItem.SelfModulate.A <= 0.05f))
            return false;

        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return true;
        if (node is RichTextLabel richTextLabel && !string.IsNullOrWhiteSpace(richTextLabel.Text))
            return true;

        for (var i = 0; i < node.GetChildCount(); i++)
        {
            if (HasVisibleText(node.GetChild(i)))
                return true;
        }

        return false;
    }

    private static void CollectText(Node node, List<string> parts)
    {
        if (node is Label label)
            AddTextPart(parts, label.Text);
        else if (node is RichTextLabel richTextLabel)
            AddTextPart(parts, Message.StripBbcode(richTextLabel.Text));

        for (var i = 0; i < node.GetChildCount(); i++)
            CollectText(node.GetChild(i), parts);
    }

    private static void AddTextPart(List<string> parts, string? value)
    {
        value = NormalizeText(value);
        if (!string.IsNullOrEmpty(value))
            parts.Add(value);
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Message.StripBbcode(value).Trim();
    }
}
