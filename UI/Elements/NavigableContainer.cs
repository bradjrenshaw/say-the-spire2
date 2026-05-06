using SayTheSpire2.Input;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI.Elements;

public class NavigableContainer : ListContainer
{
    private UIElement? _focusedChild;

    /// <summary>
    /// The element currently focused in this container, or null if focus is
    /// unset or the remembered element is no longer a child. Tracked by
    /// reference so mutations to <see cref="Children"/> (reorder / insert /
    /// remove) don't shift focus to a different element.
    /// </summary>
    public UIElement? FocusedChild =>
        _focusedChild != null && IndexOf(_focusedChild) >= 0 ? _focusedChild : null;

    /// <summary>
    /// Routes container-level focus moves (e.g. Home / End from
    /// ContainerNavigation) through the same SetFocusTo path that arrow-key
    /// navigation uses, so _focusedChild stays consistent and the
    /// announcement notify fires.
    /// </summary>
    public override void FocusChild(UIElement child)
    {
        SetFocusTo(child);
    }

    public bool HandleAction(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_down":
                return MoveRelative(1);
            case "ui_up":
                return MoveRelative(-1);
            case "ui_left":
                if (FocusedChild is RowContainer rowLeft) return rowLeft.MoveRelative(-1);
                if (FocusedChild is SliderElement slLeft) { slLeft.Decrement(); return true; }
                if (FocusedChild is NullableSliderElement nslLeft) { nslLeft.Decrement(); return true; }
                return false;
            case "ui_right":
                if (FocusedChild is RowContainer rowRight) return rowRight.MoveRelative(1);
                if (FocusedChild is SliderElement slRight) { slRight.Increment(); return true; }
                if (FocusedChild is NullableSliderElement nslRight) { nslRight.Increment(); return true; }
                return false;
            case "ui_accept":
            case "ui_select":
                return ActivateFocused();
            default:
                return false;
        }
    }

    public void FocusFirst()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsVisible)
            {
                SetFocus(Children[i]);
                return;
            }
        }
    }

    public bool MoveRelative(int direction)
    {
        if (Children.Count == 0) return false;

        int index = _focusedChild != null ? IndexOf(_focusedChild) : -1;

        while (true)
        {
            index += direction;
            if (index < 0 || index >= Children.Count)
                return true; // at boundary, consume but do nothing

            if (Children[index].IsVisible)
            {
                SetFocus(Children[index]);
                return true;
            }
        }
    }

    /// <summary>
    /// Focus the given element. Accepts either a direct child or a leaf inside
    /// a <see cref="RowContainer"/> child; falls back to <see cref="FocusFirst"/>
    /// when the element isn't reachable — e.g., after a rebuild that dropped it.
    /// </summary>
    public void SetFocusTo(UIElement element)
    {
        if (element.Parent is RowContainer row && IndexOf(row) >= 0 && row.IsVisible)
        {
            _focusedChild = row;
            row.SetFocusTo(element);
            return;
        }

        if (IndexOf(element) >= 0 && element.IsVisible)
            SetFocus(element);
        else
            FocusFirst();
    }

    private void SetFocus(UIElement element)
    {
        if (_focusedChild != null && _focusedChild != element && IndexOf(_focusedChild) >= 0)
            _focusedChild.Unfocus();

        _focusedChild = element;

        // A row wraps several focusable children — push focus inside so the
        // announced element is the actual leaf (button/etc.), not the row.
        if (element is RowContainer row)
            row.FocusCurrent();
        else
            UIManager.SetFocusedElement(element);
    }

    private bool ActivateFocused()
    {
        var child = FocusedChild;
        if (child == null) return false;

        switch (child)
        {
            case RowContainer row:
                return row.ActivateFocused();
            case ButtonElement button:
                button.Activate();
                return true;
            case CheckboxElement checkbox:
                checkbox.Activate();
                return true;
            case NullableCheckboxElement nullableCheckbox:
                nullableCheckbox.Activate();
                return true;
            case DropdownElement dropdown:
                var screen = new ChoiceSelectionScreen(dropdown.Setting);
                ScreenManager.PushScreen(screen);
                return true;
            case ActionElement action:
                return action.Activate();
            default:
                return false;
        }
    }
}
