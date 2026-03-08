using SayTheSpire2.Input;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI.Elements;

public class NavigableContainer : ListContainer
{
    private int _focusIndex = -1;

    public UIElement? FocusedChild =>
        _focusIndex >= 0 && _focusIndex < Children.Count ? Children[_focusIndex] : null;

    public bool HandleAction(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_down":
                return MoveFocus(1);
            case "ui_up":
                return MoveFocus(-1);
            case "ui_left":
                if (FocusedChild is SliderElement slLeft) { slLeft.Decrement(); return true; }
                return false;
            case "ui_right":
                if (FocusedChild is SliderElement slRight) { slRight.Increment(); return true; }
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
                SetFocus(i);
                return;
            }
        }
    }

    private bool MoveFocus(int direction)
    {
        if (Children.Count == 0) return false;

        int index = _focusIndex;

        while (true)
        {
            index += direction;
            if (index < 0 || index >= Children.Count)
                return true; // at boundary, consume but do nothing

            if (Children[index].IsVisible)
            {
                SetFocus(index);
                return true;
            }
        }
    }

    public void SetFocusTo(UIElement element)
    {
        var index = IndexOf(element);
        if (index >= 0)
            SetFocus(index);
    }

    private void SetFocus(int index)
    {
        if (_focusIndex >= 0 && _focusIndex < Children.Count)
            Children[_focusIndex].Unfocus();

        _focusIndex = index;
        UIManager.QueueFocus(Children[index]);
    }

    private bool ActivateFocused()
    {
        var child = FocusedChild;
        if (child == null) return false;

        switch (child)
        {
            case ButtonElement button:
                button.Activate();
                return true;
            case CheckboxElement checkbox:
                checkbox.Activate();
                return true;
            case DropdownElement dropdown:
                var screen = new ChoiceSelectionScreen(dropdown.Setting);
                ScreenManager.PushScreen(screen);
                return true;
            default:
                return false;
        }
    }
}
