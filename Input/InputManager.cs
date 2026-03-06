using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Input;

public static class InputManager
{
    private static readonly List<InputAction> _actions = new();

    public static void Initialize()
    {
        _actions.Add(new InputAction("buffer_next_item").AddBinding(Key.Up, ctrl: true));
        _actions.Add(new InputAction("buffer_prev_item").AddBinding(Key.Down, ctrl: true));
        _actions.Add(new InputAction("buffer_next").AddBinding(Key.Right, ctrl: true));
        _actions.Add(new InputAction("buffer_prev").AddBinding(Key.Left, ctrl: true));
        _actions.Add(new InputAction("reset_bindings").AddBinding(Key.R, ctrl: true, shift: true));

        Log.Info("[AccessibilityMod] InputManager initialized.");
    }

    public static InputAction? MatchAction(InputEventKey key)
    {
        return _actions.FirstOrDefault(a => a.Matches(key));
    }
}
