using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using ListContainer = SayTheSpire2.UI.Elements.ListContainer;

namespace SayTheSpire2.UI.Screens;

public class TimelineGameScreen : GameScreen
{
    public static TimelineGameScreen? Current { get; private set; }

    public override Message ScreenName => Message.Localized("ui", "SCREENS.TIMELINE");

    private readonly NTimelineScreen _screen;

    private static readonly FieldInfo? EpochSlotContainerField =
        AccessTools.Field(typeof(NTimelineScreen), "_epochSlotContainer");

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.TIMELINE_NAV", "Navigate epochs with directional controls. Epochs are arranged in columns by era. Press Enter to reveal an obtained epoch. You must reveal all available epochs before you can exit this screen."), exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.JUMP_TO_REVEALABLE", "Jump to Next Revealable Epoch"), "mega_top_panel"),
    };

    public TimelineGameScreen(NTimelineScreen screen)
    {
        _screen = screen;
        ClaimAction("mega_top_panel");
    }

    protected override void BuildRegistry()
    {
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

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "mega_top_panel")
        {
            FocusFirstRevealable();
            return true;
        }
        return false;
    }

    private void FocusFirstRevealable()
    {
        try
        {
            var columns = GetSortedColumns();
            if (columns == null) return;

            foreach (var (_, slots) in columns)
            {
                foreach (var slot in slots)
                {
                    if (slot.State == EpochSlotState.Obtained)
                    {
                        slot.GrabFocus();
                        return;
                    }
                }
            }

            SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_EPOCHS_READY"));
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Timeline focus revealable error: {ex.Message}");
        }
    }

    public void OnEnableInput()
    {
        try
        {
            var columns = GetSortedColumns();
            if (columns == null) return;

            if (Settings.UIEnhancementsSettings.Timeline.Get())
                FixFocusNeighbors(columns);
            BuildContainerTree(columns);
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Timeline EnableInput error: {ex.Message}");
        }
    }

    public void OnUnlockScreenOpen(NUnlockScreen instance)
    {
        try
        {
            var parts = new List<string>();

            TryAddChildText(parts, instance, "%Banner");
            TryAddRichText(parts, instance, "%ExplanationText");
            TryAddRichText(parts, instance, "%Label");
            TryAddRichText(parts, instance, "%InfoLabel");
            TryAddRichText(parts, instance, "%TopLabel");
            TryAddRichText(parts, instance, "%BottomLabel");

            var itemNames = GetUnlockedItemNames(instance);
            if (itemNames != null)
                parts.Add(itemNames);

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Unlock screen: {message}");
                SpeechManager.Output(Message.Raw(message));
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Unlock screen error: {ex.Message}");
        }
    }

    private static void TryAddChildText(List<string> parts, Node parent, string nodePath)
    {
        var node = parent.GetNodeOrNull(nodePath);
        if (node == null) return;
        var text = ProxyElement.FindChildTextPublic(node);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void TryAddRichText(List<string> parts, Node parent, string nodePath)
    {
        var node = parent.GetNodeOrNull(nodePath);
        if (node is not RichTextLabel rtl) return;
        var text = ProxyElement.StripBbcode(rtl.Text);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private List<(NEraColumn era, List<NEpochSlot> slots)>? GetSortedColumns()
    {
        var hbox = EpochSlotContainerField?.GetValue(_screen) as HBoxContainer;
        if (hbox == null) return null;

        var columns = new List<(NEraColumn era, List<NEpochSlot> slots)>();
        foreach (var child in hbox.GetChildren())
        {
            if (child is not NEraColumn eraCol) continue;
            var slots = eraCol.GetChildren().OfType<NEpochSlot>().ToList();
            if (slots.Count > 0)
            {
                slots.Sort((a, b) => a.GlobalPosition.Y.CompareTo(b.GlobalPosition.Y));
                columns.Add((eraCol, slots));
            }
        }

        return columns.Count > 0 ? columns : null;
    }

    private void BuildContainerTree(List<(NEraColumn era, List<NEpochSlot> slots)> columns)
    {
        var root = new ListContainer { AnnounceName = false, AnnouncePosition = false };

        ClearRegistry();

        foreach (var (eraCol, slots) in columns)
        {
            // Read era name from the %Name node
            var nameNode = eraCol.GetNodeOrNull<Control>("%Name");
            var eraName = nameNode != null ? ProxyElement.FindChildTextPublic(nameNode) : null;
            if (string.IsNullOrEmpty(eraName))
                eraName = eraCol.era.ToString();

            var eraContainer = new ListContainer
            {
                ContainerLabel = Message.Raw(eraName),
                AnnounceName = true,
                AnnouncePosition = true
            };

            foreach (var slot in slots)
            {
                var proxy = new ProxyEpochSlot(slot);
                eraContainer.Add(proxy);
                Register(slot, proxy);
            }

            root.Add(eraContainer);
        }

        RootElement = root;
        Log.Info($"[AccessibilityMod] Timeline container tree: {columns.Count} eras");
    }

    private static void FixFocusNeighbors(List<(NEraColumn era, List<NEpochSlot> slots)> columns)
    {
        for (int col = 0; col < columns.Count; col++)
        {
            var slots = columns[col].slots;
            for (int row = 0; row < slots.Count; row++)
            {
                var slot = slots[row];

                // Up/Down: stay within column
                slot.FocusNeighborTop = row > 0
                    ? slots[row - 1].GetPath()
                    : slot.GetPath();

                slot.FocusNeighborBottom = row < slots.Count - 1
                    ? slots[row + 1].GetPath()
                    : slot.GetPath();

                // Left/Right: same row in adjacent column (clamped)
                if (col > 0)
                {
                    var leftSlots = columns[col - 1].slots;
                    slot.FocusNeighborLeft = leftSlots[System.Math.Min(row, leftSlots.Count - 1)].GetPath();
                }
                else
                {
                    slot.FocusNeighborLeft = slot.GetPath();
                }

                if (col < columns.Count - 1)
                {
                    var rightSlots = columns[col + 1].slots;
                    slot.FocusNeighborRight = rightSlots[System.Math.Min(row, rightSlots.Count - 1)].GetPath();
                }
                else
                {
                    slot.FocusNeighborRight = slot.GetPath();
                }
            }
        }

        Log.Info($"[AccessibilityMod] Fixed timeline focus neighbors: {columns.Count} columns");
    }

    private static string? GetUnlockedItemNames(NUnlockScreen instance)
    {
        try
        {
            var type = instance.GetType();
            var names = new List<string>();

            // Cards use Title.ToString() directly; relics/potions/epochs use Title.GetFormattedText()
            CollectTitles(names, type, instance, "_cards", useFormattedText: false);
            CollectTitles(names, type, instance, "_relics", useFormattedText: true);
            CollectTitles(names, type, instance, "_potions", useFormattedText: true);
            CollectTitles(names, type, instance, "_unlockedEpochs", useFormattedText: true);

            return names.Count > 0 ? string.Join(", ", names) : null;
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to read unlock items: {ex.Message}");
            return null;
        }
    }

    private static void CollectTitles(List<string> names, System.Type type, object instance,
        string fieldName, bool useFormattedText)
    {
        var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(instance) is not System.Collections.IEnumerable items) return;

        foreach (var item in items)
        {
            var titleProp = item?.GetType().GetProperty("Title")?.GetValue(item);
            if (titleProp == null) continue;

            string? text = useFormattedText
                ? titleProp.GetType().GetMethod("GetFormattedText")?.Invoke(titleProp, null)?.ToString()
                : titleProp.ToString();

            if (!string.IsNullOrEmpty(text))
                names.Add(text);
        }
    }
}
