using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Wraps the beta-introduced NUnlockScreen family (cards / relics / potions /
/// character / misc / epoch / timeline) so navigating within the unlocked-items
/// row reads out via the existing proxy system. The unlock screens don't wire
/// FocusNeighbor* themselves, so controller / keyboard nav is dead by default.
/// We mirror the pattern NCardGrid.UpdateGridNavigation and NPlayerHand use:
/// wire neighbors on the holder (not the hitbox). The holder is the focus
/// owner in the game's own patterns; CardHolderFocusPostfix triggers off
/// NCardHolder.OnFocus, which fires whether focus enters via the holder or
/// via its hitbox's Focused signal, so a single registration on the holder
/// covers both paths.
/// </summary>
public class UnlockGameScreen : GameScreen
{
    public static UnlockGameScreen? Current { get; private set; }

    private readonly NUnlockScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "SCREENS.EPOCH_UNLOCKS"),
        AnnounceName = false,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, UIElement> _proxies = new();
    private bool _focusGrabbed;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.EPOCH_UNLOCKS");

    public UnlockGameScreen(NUnlockScreen screen)
    {
        _screen = screen;
        RootElement = _root;
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
        if (!GodotObject.IsInstanceValid(_screen) || !_screen.IsVisibleInTree())
        {
            ScreenManager.RemoveScreen(this);
            return;
        }

        BuildRegistry();
    }

    protected override void BuildRegistry()
    {
        ClearRegistry();
        _root.Clear();

        var holders = ResolveItems();
        if (holders.Count == 0) return;

        foreach (var holder in holders)
        {
            holder.FocusMode = Control.FocusModeEnum.All;

            var element = GetOrCreateElement(holder);
            _root.Add(element);
            Register(holder, element);
        }

        WireFocusNeighbors(holders);

        if (!_focusGrabbed)
        {
            _focusGrabbed = true;
            // Match NUnlockCardsScreen.DefaultFocusedControl (middle holder)
            // so the starting announcement matches what a sighted player sees.
            var middle = holders[holders.Count / 2];
            middle.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private UIElement GetOrCreateElement(Control control)
    {
        if (_proxies.TryGetValue(control, out var existing))
            return existing;
        var element = ProxyFactory.Create(control);
        _proxies[control] = element;
        return element;
    }

    private static void WireFocusNeighbors(List<Control> children)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var self = children[i].GetPath();
            children[i].FocusNeighborLeft = i > 0 ? children[i - 1].GetPath() : children[^1].GetPath();
            children[i].FocusNeighborRight = i < children.Count - 1 ? children[i + 1].GetPath() : children[0].GetPath();
            children[i].FocusNeighborTop = self;
            children[i].FocusNeighborBottom = self;
        }
    }

    /// <summary>
    /// Look up the row container and filter to the canonical holder type for
    /// the concrete screen subclass. Skips queue-freed nodes (relevant when
    /// re-opening unlock screens since the previous instance's _ExitTree
    /// QueueFrees its holders rather than removing them synchronously).
    /// </summary>
    private List<Control> ResolveItems()
    {
        var (row, kind) = _screen switch
        {
            NUnlockCardsScreen => (_screen.GetNodeOrNull<Control>("%CardRow"), "card"),
            NUnlockRelicsScreen => (_screen.GetNodeOrNull<Control>("%RelicRow"), "relic"),
            NUnlockPotionsScreen => (_screen.GetNodeOrNull<Control>("%PotionRow"), "potion"),
            _ => (null, null),
        };
        if (row == null || kind == null) return new List<Control>();

        return row.GetChildren()
            .OfType<Control>()
            .Where(c => GodotObject.IsInstanceValid(c) && c.Visible && !c.IsQueuedForDeletion())
            .Where(c => kind switch
            {
                "card" => c is NCardHolder,
                "relic" => c is NRelicBasicHolder,
                "potion" => c is NPotionHolder,
                _ => false,
            })
            .ToList();
    }
}
