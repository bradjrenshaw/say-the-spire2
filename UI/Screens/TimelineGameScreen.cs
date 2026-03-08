using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using ListContainer = SayTheSpire2.UI.Elements.ListContainer;

namespace SayTheSpire2.UI.Screens;

public class TimelineGameScreen : GameScreen
{
    public static TimelineGameScreen? Current { get; private set; }

    public override string ScreenName => "Timeline";

    private readonly NTimelineScreen _screen;

    private static readonly FieldInfo? EpochSlotContainerField =
        typeof(NTimelineScreen).GetField("_epochSlotContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    public TimelineGameScreen(NTimelineScreen screen)
    {
        _screen = screen;
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

    public void OnEnableInput()
    {
        try
        {
            var columns = GetSortedColumns();
            if (columns == null) return;

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

            // Try %Banner (relics, cards, potions screens)
            var banner = instance.GetNodeOrNull("%Banner");
            if (banner != null)
            {
                var bannerText = ProxyElement.FindChildTextPublic(banner);
                if (!string.IsNullOrEmpty(bannerText))
                    parts.Add(bannerText);
            }

            // Try %ExplanationText (relics, cards, potions screens)
            var explanation = instance.GetNodeOrNull("%ExplanationText");
            if (explanation is RichTextLabel explRtl)
            {
                var text = ProxyElement.StripBbcode(explRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %Label (misc screen)
            var label = instance.GetNodeOrNull("%Label");
            if (label is RichTextLabel labelRtl)
            {
                var text = ProxyElement.StripBbcode(labelRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %InfoLabel (epoch screen)
            var infoLabel = instance.GetNodeOrNull("%InfoLabel");
            if (infoLabel is RichTextLabel infoRtl)
            {
                var text = ProxyElement.StripBbcode(infoRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %TopLabel and %BottomLabel (character unlock screen)
            var topLabel = instance.GetNodeOrNull("%TopLabel");
            if (topLabel is RichTextLabel topRtl)
            {
                var text = ProxyElement.StripBbcode(topRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            var bottomLabel = instance.GetNodeOrNull("%BottomLabel");
            if (bottomLabel is RichTextLabel bottomRtl)
            {
                var text = ProxyElement.StripBbcode(bottomRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Read unlocked item names via reflection
            var itemNames = GetUnlockedItemNames(instance);
            if (itemNames != null)
                parts.Add(itemNames);

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Unlock screen: {message}");
                SpeechManager.Output(Localization.Message.Raw(message));
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Unlock screen error: {ex.Message}");
        }
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
                slots.Sort((a, b) => a.eraPosition.CompareTo(b.eraPosition));
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
                ContainerLabel = eraName,
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
        FocusContext?.Reset();
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

            // Try _cards field (NUnlockCardsScreen)
            var cardsField = type.GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cardsField?.GetValue(instance) is System.Collections.IEnumerable cards)
            {
                foreach (var card in cards)
                {
                    var title = card?.GetType().GetProperty("Title")?.GetValue(card)?.ToString();
                    if (!string.IsNullOrEmpty(title))
                        names.Add(title);
                }
            }

            // Try _relics field (NUnlockRelicsScreen)
            var relicsField = type.GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic);
            if (relicsField?.GetValue(instance) is System.Collections.IEnumerable relics)
            {
                foreach (var relic in relics)
                {
                    var title = relic?.GetType().GetProperty("Title")?.GetValue(relic);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            // Try _potions field (NUnlockPotionsScreen)
            var potionsField = type.GetField("_potions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (potionsField?.GetValue(instance) is System.Collections.IEnumerable potions)
            {
                foreach (var potion in potions)
                {
                    var title = potion?.GetType().GetProperty("Title")?.GetValue(potion);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            // Try _unlockedEpochs field (NUnlockEpochScreen)
            var epochsField = type.GetField("_unlockedEpochs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (epochsField?.GetValue(instance) is System.Collections.IEnumerable epochs)
            {
                foreach (var epoch in epochs)
                {
                    var title = epoch?.GetType().GetProperty("Title")?.GetValue(epoch);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : null;
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to read unlock items: {ex.Message}");
            return null;
        }
    }
}
