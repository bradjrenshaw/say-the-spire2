using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Credits;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Audio narration for the auto-scrolling credits screen. The credits screen
/// has no focusable inner controls and 100% hardcoded structure on the game
/// side (NCreditsScreen._Ready is a sequence of bespoke Init* methods, no
/// data model). We observe the live scene: every frame, every label whose
/// top edge has just crossed into the viewport gets announced. Within a
/// frame's batch we group by Y and, when columns share line counts, interleave
/// line-by-line so two-column "role / role / role" + "name / name / name"
/// reads as "role: name. role: name. role: name." rather than dumping all
/// roles before all names.
/// </summary>
public class CreditsGameScreen : GameScreen
{
    public static CreditsGameScreen? Current { get; private set; }

    private readonly NCreditsScreen _screen;
    private readonly List<LabelEntry> _labels = new();

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.CREDITS");

    public CreditsGameScreen(NCreditsScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
        CollectLabels();
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen) || !_screen.IsVisibleInTree())
        {
            ScreenManager.RemoveScreen(this);
            return;
        }

        AnnounceNewlyVisible();
    }

    protected override void BuildRegistry()
    {
        // No focusable controls to register — narration is observation-only.
    }

    private void CollectLabels()
    {
        _labels.Clear();
        // Prefer the dedicated scrolling content node so we skip the BackButton
        // label and any other chrome. If the scene gets renamed in a future
        // update, fall back to walking the full screen — the type filter
        // (Label / RichTextLabel) is the stable contract.
        var root = _screen.GetNodeOrNull<Control>("%ScreenContents") ?? (Control)_screen;
        WalkTree(root);
    }

    private void WalkTree(Node node)
    {
        if (node is Label or RichTextLabel)
            _labels.Add(new LabelEntry((Control)node));
        foreach (var child in node.GetChildren())
            WalkTree(child);
    }

    private void AnnounceNewlyVisible()
    {
        var viewportBottom = _screen.GetViewportRect().Size.Y;

        var newlyVisible = _labels
            .Where(l => !l.Announced
                && GodotObject.IsInstanceValid(l.Node)
                && l.Node.IsVisibleInTree()
                && l.Node.GlobalPosition.Y <= viewportBottom)
            .ToList();
        if (newlyVisible.Count == 0) return;

        // Group rows by Y (rounded to absorb sub-pixel drift), order rows
        // top-to-bottom, columns within each row left-to-right.
        var rows = newlyVisible
            .GroupBy(l => Mathf.RoundToInt(l.Node.GlobalPosition.Y))
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(l => l.Node.GlobalPosition.X).ToList());

        foreach (var row in rows)
        {
            AnnounceRow(row);
            foreach (var l in row) l.Announced = true;
        }
    }

    private static void AnnounceRow(List<LabelEntry> row)
    {
        var texts = row.Select(l => GetText(l.Node)).ToList();

        if (texts.Count == 1)
        {
            Speak(texts[0]);
            return;
        }

        // Try line-pairing: if every column has the same line count > 1, the
        // row is a column-aligned multi-line block. Speak by line index so
        // "Director / Producer" + "Alice / Bob" reads "Director: Alice.
        // Producer: Bob." Empty cells (e.g. multi-role continuation lines in
        // the Voices section) drop out of the join.
        var splits = texts.Select(t => t.Split('\n')).ToList();
        int lineCount = splits[0].Length;
        bool aligned = lineCount > 1 && splits.All(s => s.Length == lineCount);

        if (aligned)
        {
            for (int i = 0; i < lineCount; i++)
            {
                var parts = splits
                    .Select(s => s[i].Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                if (parts.Count > 0)
                    Speak(string.Join(": ", parts));
            }
        }
        else
        {
            // Mismatched line counts — fall back to left-to-right per label.
            foreach (var t in texts)
                Speak(t);
        }
    }

    private static string GetText(Control label)
    {
        var raw = label switch
        {
            RichTextLabel rtl => rtl.Text,
            Label l => l.Text,
            _ => "",
        };
        return ProxyElement.StripBbcode(raw)?.Trim() ?? "";
    }

    private static void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        SpeechManager.Output(Message.Raw(text), interrupt: false);
    }

    private class LabelEntry
    {
        public Control Node;
        public bool Announced;
        public LabelEntry(Control node) { Node = node; Announced = false; }
    }
}
