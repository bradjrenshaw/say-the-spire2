using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI;

/// <summary>
/// Home / End navigation helpers. Reads the mod's logical focus
/// (<see cref="UIManager.CurrentElement"/>) so it stays correct on screens
/// that bookkeep focus internally rather than through Godot — e.g. the
/// NavigableContainer used by Mod Settings and Mod Menu — then asks the
/// focused element's parent <see cref="Container"/> to focus the first or
/// last visible sibling. The container chooses how to focus (GrabFocus on
/// the backing Control, or SetFocusTo / SetFocusedElement for logical
/// focus), so the same call works uniformly across both modalities.
/// </summary>
public static class ContainerNavigation
{
    public static bool JumpToFirst() => JumpInContainer(toFirst: true);
    public static bool JumpToLast() => JumpInContainer(toFirst: false);

    private static bool JumpInContainer(bool toFirst)
    {
        try
        {
            var current = UIManager.CurrentElement;
            if (current?.Parent == null) return false;

            var visible = current.Parent.Children.Where(c => c.IsVisible).ToList();
            if (visible.Count == 0) return false;

            var target = toFirst ? visible[0] : visible[^1];
            if (target == current) return false;

            current.Parent.FocusChild(target);
            return true;
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] ContainerNavigation jump failed: {e.Message}");
            return false;
        }
    }
}
