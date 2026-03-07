using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Input;

public static class InputManager
{
    private static readonly List<InputAction> _actions = new();
    private static readonly HashSet<InputAction> _activeActions = new();

    private static readonly HashSet<Key> _modifierKeys = new()
    {
        Key.Ctrl, Key.Shift, Key.Alt,
    };

    private static readonly HashSet<string> _navActions = new()
    {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "ui_accept", "ui_cancel", "ui_select"
    };

    /// <summary>
    /// When true, the mod intercepts all keyboard input and suppresses the game's
    /// default handling. Set to false to let the game handle input normally.
    /// </summary>
    public static bool InterceptInput { get; set; } = true;

    private static NControllerManager? _controllerManager;

    private static readonly FieldInfo? ListeningEntryField =
        typeof(NInputSettingsPanel).GetField("_listeningEntry", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo IsUsingControllerProp =
        typeof(NControllerManager).GetProperty("IsUsingController", BindingFlags.Instance | BindingFlags.Public)!;

    private static readonly FieldInfo? LastMouseField =
        AccessTools.Field(typeof(NControllerManager), "_lastMousePosition");

    public static void Initialize()
    {
        RegisterGameActions();
        RegisterModActions();
        Log.Info($"[AccessibilityMod] InputManager initialized with {_actions.Count} actions.");
    }

    private static void RegisterGameActions()
    {
        var gameBindings = new Dictionary<string, Key>
        {
            { "ui_accept", Key.E },
            { "ui_select", Key.Enter },
            { "ui_cancel", Key.Escape },
            { "ui_up", Key.Up },
            { "ui_down", Key.Down },
            { "ui_left", Key.Left },
            { "ui_right", Key.Right },
            { "mega_peek", Key.Space },
            { "mega_view_draw_pile", Key.A },
            { "mega_view_discard_pile", Key.S },
            { "mega_view_deck_and_tab_left", Key.D },
            { "mega_view_exhaust_pile_and_tab_right", Key.X },
            { "mega_view_map", Key.M },
            { "mega_pause_and_back", Key.Escape },
            { "mega_top_panel", Key.Tab },
            { "mega_select_card_1", Key.Key1 },
            { "mega_select_card_2", Key.Key2 },
            { "mega_select_card_3", Key.Key3 },
            { "mega_select_card_4", Key.Key4 },
            { "mega_select_card_5", Key.Key5 },
            { "mega_select_card_6", Key.Key6 },
            { "mega_select_card_7", Key.Key7 },
            { "mega_select_card_8", Key.Key8 },
            { "mega_select_card_9", Key.Key9 },
            { "mega_select_card_10", Key.Key0 },
            { "mega_release_card", Key.Down },
        };

        foreach (var (actionName, key) in gameBindings)
        {
            _actions.Add(new InputAction(actionName, gameAction: actionName).AddBinding(key));
        }
    }

    private static void RegisterModActions()
    {
        _actions.Add(new InputAction("buffer_next_item").AddBinding(Key.Up, ctrl: true));
        _actions.Add(new InputAction("buffer_prev_item").AddBinding(Key.Down, ctrl: true));
        _actions.Add(new InputAction("buffer_next").AddBinding(Key.Right, ctrl: true));
        _actions.Add(new InputAction("buffer_prev").AddBinding(Key.Left, ctrl: true));
        _actions.Add(new InputAction("reset_bindings").AddBinding(Key.R, ctrl: true, shift: true));
        _actions.Add(new InputAction("announce_gold").AddBinding(Key.G, ctrl: true));
        _actions.Add(new InputAction("announce_hp").AddBinding(Key.H, ctrl: true));
        _actions.Add(new InputAction("announce_block").AddBinding(Key.B, ctrl: true));
        _actions.Add(new InputAction("announce_energy").AddBinding(Key.Y, ctrl: true));
        _actions.Add(new InputAction("announce_powers").AddBinding(Key.P, ctrl: true));
        _actions.Add(new InputAction("announce_intents").AddBinding(Key.I, ctrl: true));
        _actions.Add(new InputAction("mod_settings").AddBinding(Key.M, ctrl: true));
    }

    /// <summary>
    /// Called from _Input prefix on NControllerManager. Updates key states,
    /// matches actions immediately, and consumes the event.
    /// Returns true if the event was consumed.
    /// </summary>
    public static bool OnInputEvent(NControllerManager controller, InputEvent inputEvent)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return false;

        _controllerManager = controller;

        if (inputEvent is InputEventKey keyEvent)
        {
            if (keyEvent.Echo)
                return true; // consume but don't process

            if (keyEvent.Pressed)
                OnKeyPressed(keyEvent);
            else
                OnKeyReleased(keyEvent);

            return true;
        }

        // Let non-keyboard events through for now (controller deferred)
        return false;
    }

    private static void OnKeyPressed(InputEventKey keyEvent)
    {
        // If it's a modifier key, don't trigger actions — just wait for the
        // non-modifier key that completes the combo
        if (_modifierKeys.Contains(keyEvent.Keycode))
            return;

        // Find matching actions based on keycode + current modifiers
        bool anyConsumed = false;
        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesKeyEvent(keyEvent))
            {
                _activeActions.Add(action);
                EnsureFocusMode(action);

                // If any action for this key was consumed by the screen stack,
                // don't inject game actions for remaining matches either
                if (!anyConsumed)
                {
                    bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustPressed);
                    if (consumed)
                        anyConsumed = true;
                    else if (action.GameAction != null)
                        InjectGameAction(action.GameAction, pressed: true);
                }
            }
        }
    }

    private static void OnKeyReleased(InputEventKey keyEvent)
    {
        // Check which active actions are no longer satisfied
        // (released key was part of their binding)
        var toRelease = new List<InputAction>();
        foreach (var action in _activeActions)
        {
            if (action.UsesKey(keyEvent.Keycode))
                toRelease.Add(action);
        }

        foreach (var action in toRelease)
        {
            _activeActions.Remove(action);
            bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustReleased);
            if (!consumed && action.GameAction != null)
                InjectGameAction(action.GameAction, pressed: false);
        }
    }

    private static bool IsGameListeningForRebind()
    {
        if (ListeningEntryField == null)
            return false;

        var screen = ActiveScreenContext.Instance.GetCurrentScreen();
        if (screen is not NInputSettingsPanel panel)
            return false;

        return ListeningEntryField.GetValue(panel) != null;
    }

    private static void EnsureFocusMode(InputAction action)
    {
        if (_controllerManager == null)
            return;

        if (!_navActions.Contains(action.Key))
            return;

        bool isUsingController = (bool)IsUsingControllerProp.GetValue(_controllerManager)!;
        if (isUsingController)
            return;

        IsUsingControllerProp.SetValue(_controllerManager, true);

        var viewport = _controllerManager.GetViewport();
        if (viewport != null)
        {
            var mousePos = DisplayServer.MouseGetPosition();
            var windowPos = DisplayServer.WindowGetPosition();
            var localMouse = new Vector2(mousePos.X - windowPos.X, mousePos.Y - windowPos.Y);
            LastMouseField?.SetValue(_controllerManager, localMouse);
            viewport.WarpMouse(Vector2.One * -1000f);
        }

        ActiveScreenContext.Instance.FocusOnDefaultControl();
        _controllerManager.EmitSignal("ControllerDetected");

        Log.Info("[AccessibilityMod] Keyboard nav: switched to focus mode");
    }

    private static void InjectGameAction(string actionName, bool pressed)
    {
        var inputEventAction = new InputEventAction
        {
            Action = actionName,
            Pressed = pressed
        };
        Godot.Input.ParseInputEvent(inputEventAction);
    }
}
