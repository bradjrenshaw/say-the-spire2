using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Input;

public static class InputManager
{
    private static readonly List<InputAction> _actions = new();
    public static IReadOnlyList<InputAction> Actions => _actions;
    private static readonly HashSet<InputAction> _activeActions = new();
    private static readonly HashSet<ControllerInput> _heldControllerInputs = new();
    private static readonly Dictionary<string, List<InputBinding>> _defaultBindings = new();

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

    private static System.Action<InputBinding>? _listenCallback;
    private static HashSet<ControllerInput>? _listenInitialHeld;
    public static bool IsListening => _listenCallback != null;

    public static void StartListening(System.Action<InputBinding> callback)
    {
        _listenCallback = callback;
        // Snapshot currently held buttons so we ignore their release
        _listenInitialHeld = new HashSet<ControllerInput>(_heldControllerInputs);
    }

    public static void StopListening()
    {
        _listenCallback = null;
        _listenInitialHeld = null;
    }

    private static NControllerManager? _controllerManager;

    private static readonly FieldInfo? ListeningEntryField =
        typeof(NInputSettingsPanel).GetField("_listeningEntry", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo IsUsingControllerProp =
        typeof(NControllerManager).GetProperty("IsUsingController", BindingFlags.Instance | BindingFlags.Public)!;

    private static readonly FieldInfo? LastMouseField =
        AccessTools.Field(typeof(NControllerManager), "_lastMousePosition");

    public static void Initialize()
    {
        RegisterCustomInputMapActions();
        RegisterGameActions();
        RegisterModActions();
        // Snapshot default bindings so we can reset later
        foreach (var action in _actions)
            _defaultBindings[action.Key] = new List<InputBinding>(action.Bindings);
        Log.Info($"[AccessibilityMod] InputManager initialized with {_actions.Count} actions.");
    }

    public static void ResetToDefaults()
    {
        foreach (var action in _actions)
        {
            if (!_defaultBindings.TryGetValue(action.Key, out var defaults))
                continue;
            action.ClearBindings();
            foreach (var binding in defaults)
                action.AddBinding(binding);
        }
        Settings.ModSettings.MarkDirty();
        Log.Info("[AccessibilityMod] Mod keybindings reset to defaults.");
    }

    /// <summary>
    /// Modify Godot input map so the game doesn't also handle right stick click
    /// (it maps both stick clicks to controller_joystick_press by default).
    /// </summary>
    private static void RegisterCustomInputMapActions()
    {
        try
        {
            if (!InputMap.HasAction("controller_joystick_press")) return;
            foreach (var evt in InputMap.ActionGetEvents("controller_joystick_press"))
            {
                if (evt is InputEventJoypadButton joyEvt && joyEvt.ButtonIndex == JoyButton.RightStick)
                {
                    InputMap.ActionEraseEvent("controller_joystick_press", evt);
                    Log.Info("[AccessibilityMod] Removed RightStick from controller_joystick_press");
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to modify controller_joystick_press: {e.Message}");
        }
    }

    private static void RegisterGameActions()
    {
        _actions.Add(new InputAction("ui_accept", "Accept", gameAction: "ui_accept")
            .AddBinding(Key.E)
            .AddBinding(ControllerInput.Y));
        _actions.Add(new InputAction("ui_select", "Select", gameAction: "ui_select")
            .AddBinding(Key.Enter)
            .AddBinding(ControllerInput.A));
        _actions.Add(new InputAction("ui_cancel", "Cancel", gameAction: "ui_cancel")
            .AddBinding(Key.Backspace)
            .AddBinding(ControllerInput.B));
        _actions.Add(new InputAction("ui_up", "Navigate Up", gameAction: "ui_up")
            .AddBinding(Key.Up)
            .AddBinding(ControllerInput.DpadUp)
            .AddBinding(ControllerInput.LeftStickUp));
        _actions.Add(new InputAction("ui_down", "Navigate Down", gameAction: "ui_down")
            .AddBinding(Key.Down)
            .AddBinding(ControllerInput.DpadDown)
            .AddBinding(ControllerInput.LeftStickDown));
        _actions.Add(new InputAction("ui_left", "Navigate Left", gameAction: "ui_left")
            .AddBinding(Key.Left)
            .AddBinding(ControllerInput.DpadLeft)
            .AddBinding(ControllerInput.LeftStickLeft));
        _actions.Add(new InputAction("ui_right", "Navigate Right", gameAction: "ui_right")
            .AddBinding(Key.Right)
            .AddBinding(ControllerInput.DpadRight)
            .AddBinding(ControllerInput.LeftStickRight));
        _actions.Add(new InputAction("mega_peek", "Peek", gameAction: "mega_peek")
            .AddBinding(Key.Space)
            .AddBinding(ControllerInput.LeftStickClick));
        _actions.Add(new InputAction("mega_view_draw_pile", "View Draw Pile", gameAction: "mega_view_draw_pile")
            .AddBinding(Key.A)
            .AddBinding(ControllerInput.LeftShoulder, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("mega_view_discard_pile", "View Discard Pile", gameAction: "mega_view_discard_pile")
            .AddBinding(Key.S)
            .AddBinding(ControllerInput.RightShoulder, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("mega_view_deck_and_tab_left", "View Deck / Tab Left", gameAction: "mega_view_deck_and_tab_left")
            .AddBinding(Key.D)
            .AddBinding(ControllerInput.LeftShoulder));
        _actions.Add(new InputAction("mega_view_exhaust_pile_and_tab_right", "View Exhaust / Tab Right", gameAction: "mega_view_exhaust_pile_and_tab_right")
            .AddBinding(Key.F)
            .AddBinding(ControllerInput.RightShoulder));
        _actions.Add(new InputAction("mega_view_map", "View Map", gameAction: "mega_view_map")
            .AddBinding(Key.M)
            .AddBinding(ControllerInput.Back));
        _actions.Add(new InputAction("mega_pause_and_back", "Pause / Back", gameAction: "mega_pause_and_back")
            .AddBinding(Key.Escape)
            .AddBinding(ControllerInput.Start));
        _actions.Add(new InputAction("mega_top_panel", "Top Panel", gameAction: "mega_top_panel")
            .AddBinding(Key.T)
            .AddBinding(ControllerInput.X));
        _actions.Add(new InputAction("mega_select_card_1", "Creature Status 1", gameAction: "mega_select_card_1").AddBinding(Key.Key1));
        _actions.Add(new InputAction("mega_select_card_2", "Creature Status 2", gameAction: "mega_select_card_2").AddBinding(Key.Key2));
        _actions.Add(new InputAction("mega_select_card_3", "Creature Status 3", gameAction: "mega_select_card_3").AddBinding(Key.Key3));
        _actions.Add(new InputAction("mega_select_card_4", "Creature Status 4", gameAction: "mega_select_card_4").AddBinding(Key.Key4));
        _actions.Add(new InputAction("mega_select_card_5", "Creature Status 5", gameAction: "mega_select_card_5").AddBinding(Key.Key5));
        _actions.Add(new InputAction("mega_select_card_6", "Creature Status 6", gameAction: "mega_select_card_6").AddBinding(Key.Key6));
        _actions.Add(new InputAction("mega_select_card_7", "Creature Status 7", gameAction: "mega_select_card_7").AddBinding(Key.Key7));
        _actions.Add(new InputAction("mega_select_card_8", "Creature Status 8", gameAction: "mega_select_card_8").AddBinding(Key.Key8));
        _actions.Add(new InputAction("mega_select_card_9", "Creature Status 9", gameAction: "mega_select_card_9").AddBinding(Key.Key9));
        _actions.Add(new InputAction("mega_select_card_10", "Creature Status 10", gameAction: "mega_select_card_10").AddBinding(Key.Key0));
        _actions.Add(new InputAction("mega_select_card_11", "Creature Status 11").AddBinding(Key.Minus));
        _actions.Add(new InputAction("mega_select_card_12", "Creature Status 12").AddBinding(Key.Equal));
        _actions.Add(new InputAction("mega_release_card", "Release Card", gameAction: "mega_release_card").AddBinding(Key.Down));
    }

    private static void RegisterModActions()
    {
        _actions.Add(new InputAction("buffer_next_item", "Buffer Next Item").AddBinding(Key.Up, ctrl: true)
            .AddBinding(ControllerInput.RightStickUp));
        _actions.Add(new InputAction("buffer_prev_item", "Buffer Previous Item").AddBinding(Key.Down, ctrl: true)
            .AddBinding(ControllerInput.RightStickDown));
        _actions.Add(new InputAction("buffer_next", "Next Buffer").AddBinding(Key.Right, ctrl: true)
            .AddBinding(ControllerInput.RightStickRight));
        _actions.Add(new InputAction("buffer_prev", "Previous Buffer").AddBinding(Key.Left, ctrl: true)
            .AddBinding(ControllerInput.RightStickLeft));
        _actions.Add(new InputAction("map_poi_prev", Ui("MAP_POI.ACTION_PREVIOUS", "Previous Point of Interest"))
            .AddBinding(Key.Comma)
            .AddBinding(ControllerInput.RightStickLeft, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_poi_next", Ui("MAP_POI.ACTION_NEXT", "Next Point of Interest"))
            .AddBinding(Key.Period)
            .AddBinding(ControllerInput.RightStickRight, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_poi_toggle_mode", Ui("MAP_POI.ACTION_TOGGLE_MODE", "Toggle Point of Interest Mode"))
            .AddBinding(Key.Backslash)
            .AddBinding(ControllerInput.RightStickUp, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_toggle_current_marker", Ui("MAP_MARKERS.ACTION_TOGGLE_CURRENT", "Toggle Current Marker"))
            .AddBinding(Key.Slash));
        _actions.Add(new InputAction("map_clear_all_markers", Ui("MAP_MARKERS.ACTION_CLEAR_ALL", "Clear All Markers"))
            .AddBinding(Key.Slash, ctrl: true, shift: true));
        _actions.Add(new InputAction("help", "Help").AddBinding(Key.F1));
        _actions.Add(new InputAction("reset_bindings", "Reset Bindings").AddBinding(Key.R, ctrl: true, shift: true));
        _actions.Add(new InputAction("announce_gold", "Announce Gold").AddBinding(Key.G, ctrl: true)
            .AddBinding(ControllerInput.A, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_hp", "Announce HP").AddBinding(Key.H, ctrl: true)
            .AddBinding(ControllerInput.A, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_block", "Announce Block").AddBinding(Key.B, ctrl: true)
            .AddBinding(ControllerInput.B, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_energy", "Announce Energy").AddBinding(Key.Y, ctrl: true)
            .AddBinding(ControllerInput.X, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_powers", "Announce Powers").AddBinding(Key.P, ctrl: true)
            .AddBinding(ControllerInput.Y, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_intents", "Announce Intents").AddBinding(Key.I, ctrl: true)
            .AddBinding(ControllerInput.Y, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_summarized_intents", "Announce Summarized Intents").AddBinding(Key.I, alt: true)
            .AddBinding(ControllerInput.X, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_boss", "Announce Boss").AddBinding(Key.N, ctrl: true)
            .AddBinding(ControllerInput.B, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_relic_counters", "Announce Relic Counters").AddBinding(Key.R, ctrl: true)
            .AddBinding(ControllerInput.Back, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("mod_settings", "Mod Settings").AddBinding(Key.M, ctrl: true)
            .AddBinding(ControllerInput.Start, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_combatant_intent_1", "Combatant Intent 1").AddBinding(Key.Key1, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_2", "Combatant Intent 2").AddBinding(Key.Key2, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_3", "Combatant Intent 3").AddBinding(Key.Key3, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_4", "Combatant Intent 4").AddBinding(Key.Key4, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_5", "Combatant Intent 5").AddBinding(Key.Key5, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_6", "Combatant Intent 6").AddBinding(Key.Key6, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_7", "Combatant Intent 7").AddBinding(Key.Key7, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_8", "Combatant Intent 8").AddBinding(Key.Key8, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_9", "Combatant Intent 9").AddBinding(Key.Key9, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_10", "Combatant Intent 10").AddBinding(Key.Key0, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_11", "Combatant Intent 11").AddBinding(Key.Minus, shift: true));
        _actions.Add(new InputAction("announce_combatant_intent_12", "Combatant Intent 12").AddBinding(Key.Equal, shift: true));
    }

    private static string Ui(string key, string fallback)
    {
        return LocalizationManager.GetOrDefault("ui", key, fallback);
    }

    /// <summary>
    /// Controller buttons polled from device 0 via IsJoyButtonPressed.
    /// Requires Steam Input to be disabled for the game.
    /// </summary>
    private static readonly Dictionary<JoyButton, ControllerInput> _polledButtons = new()
    {
        { JoyButton.DpadUp, ControllerInput.DpadUp },
        { JoyButton.DpadDown, ControllerInput.DpadDown },
        { JoyButton.DpadLeft, ControllerInput.DpadLeft },
        { JoyButton.DpadRight, ControllerInput.DpadRight },
        { JoyButton.A, ControllerInput.A },
        { JoyButton.B, ControllerInput.B },
        { JoyButton.X, ControllerInput.X },
        { JoyButton.Y, ControllerInput.Y },
        { JoyButton.LeftShoulder, ControllerInput.LeftShoulder },
        { JoyButton.RightShoulder, ControllerInput.RightShoulder },
        { JoyButton.LeftStick, ControllerInput.LeftStickClick },
        { JoyButton.Start, ControllerInput.Start },
        { JoyButton.Back, ControllerInput.Back },
    };

    private static readonly HashSet<JoyButton> _activePolledButtons = new();

    /// <summary>
    /// Controller axes polled from device 0 via GetJoyAxis.
    /// Covers both sticks and triggers.
    /// </summary>
    private static readonly Dictionary<(JoyAxis, bool), ControllerInput> _polledAxes = new()
    {
        { (JoyAxis.LeftX, false), ControllerInput.LeftStickLeft },
        { (JoyAxis.LeftX, true), ControllerInput.LeftStickRight },
        { (JoyAxis.LeftY, false), ControllerInput.LeftStickUp },
        { (JoyAxis.LeftY, true), ControllerInput.LeftStickDown },
        { (JoyAxis.RightX, false), ControllerInput.RightStickLeft },
        { (JoyAxis.RightX, true), ControllerInput.RightStickRight },
        { (JoyAxis.RightY, false), ControllerInput.RightStickUp },
        { (JoyAxis.RightY, true), ControllerInput.RightStickDown },
        { (JoyAxis.TriggerLeft, true), ControllerInput.LeftTrigger },
        { (JoyAxis.TriggerRight, true), ControllerInput.RightTrigger },
    };

    private static readonly HashSet<(JoyAxis, bool)> _activePolledAxes = new();
    private const float StickDeadzone = 0.5f;

    /// <summary>
    /// Called from _Input prefix on NControllerManager.
    /// Handles keyboard events. Controller events don't arrive here.
    /// </summary>
    public static bool OnInputEvent(NControllerManager controller, InputEvent inputEvent)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return false;

        _controllerManager = controller;

        if (inputEvent is InputEventKey keyEvent)
        {
            if (ShouldLetFocusedTextControlHandleKey(controller, keyEvent))
                return false;

            if (keyEvent.Echo)
                return true;

            if (keyEvent.Pressed)
                OnKeyPressed(keyEvent);
            else
                OnKeyReleased(keyEvent);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Called from _Process postfix on NControllerManager. Polls all controller
    /// buttons and axes directly from hardware, bypassing the game's input system.
    /// </summary>
    public static void PollCustomActions(NControllerManager controller)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return;

        _controllerManager = controller;

        try
        {
            // Poll buttons from device 0
            foreach (var (button, controllerInput) in _polledButtons)
            {
                bool isPressed = Godot.Input.IsJoyButtonPressed(0, button);
                bool wasPressed = _activePolledButtons.Contains(button);

                if (isPressed && !wasPressed)
                {
                    _activePolledButtons.Add(button);
                    OnControllerInputPressed(controllerInput);
                }
                else if (!isPressed && wasPressed)
                {
                    _activePolledButtons.Remove(button);
                    OnControllerInputReleased(controllerInput);
                }
            }

            // Poll analog sticks via raw axis values (these work with Steam Input)
            foreach (var ((axis, positive), controllerInput) in _polledAxes)
            {
                float value = Godot.Input.GetJoyAxis(0, axis);

                bool isPressed = positive ? value > StickDeadzone : value < -StickDeadzone;
                var key = (axis, positive);
                bool wasPressed = _activePolledAxes.Contains(key);

                if (isPressed && !wasPressed)
                {
                    _activePolledAxes.Add(key);
                    OnControllerInputPressed(controllerInput);
                }
                else if (!isPressed && wasPressed)
                {
                    _activePolledAxes.Remove(key);
                    OnControllerInputReleased(controllerInput);
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] PollCustomActions CRASHED: {e}");
        }
    }

    private static void OnKeyPressed(InputEventKey keyEvent)
    {
        Speech.SpeechManager.Silence();

        if (_modifierKeys.Contains(keyEvent.Keycode))
            return;

        if (IsListening)
        {
            _listenCallback?.Invoke(new KeyboardBinding(
                keyEvent.Keycode, keyEvent.CtrlPressed, keyEvent.ShiftPressed, keyEvent.AltPressed));
            return;
        }

        bool anyConsumed = false;
        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesKeyEvent(keyEvent))
            {
                _activeActions.Add(action);
                EnsureFocusMode(action);

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

    private static bool IsControllerInputHeld(ControllerInput input) => _heldControllerInputs.Contains(input);

    private static bool OnControllerInputPressed(ControllerInput input)
    {
        Speech.SpeechManager.Silence();
        _heldControllerInputs.Add(input);

        if (IsListening)
        {
            // Don't capture on press — wait for release so modifiers can be held first
            return true;
        }

        bool anyConsumed = false;
        InputAction? unmatchedFallback = null;

        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesControllerInput(input, IsControllerInputHeld))
            {
                if (action.HasControllerModifier)
                {
                    _activeActions.Add(action);
                    if (!anyConsumed)
                    {
                        bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustPressed);
                        if (consumed)
                            anyConsumed = true;
                        else if (action.GameAction != null)
                            InjectGameAction(action.GameAction, pressed: true);
                    }
                    unmatchedFallback = null;
                    break;
                }
                else if (unmatchedFallback == null)
                {
                    unmatchedFallback = action;
                }
            }
        }

        if (unmatchedFallback != null)
        {
            _activeActions.Add(unmatchedFallback);
            if (!anyConsumed)
            {
                bool consumed = ScreenManager.DispatchAction(unmatchedFallback, InputActionState.JustPressed);
                if (consumed)
                    anyConsumed = true;
                else if (unmatchedFallback.GameAction != null)
                    InjectGameAction(unmatchedFallback.GameAction, pressed: true);
            }
        }

        return anyConsumed;
    }

    private static bool OnControllerInputReleased(ControllerInput input)
    {
        _heldControllerInputs.Remove(input);

        if (IsListening)
        {
            // Ignore releases of buttons that were already held when listening started
            if (_listenInitialHeld != null && _listenInitialHeld.Remove(input))
                return true;

            // Released button is the main input, anything still held is the modifier
            ControllerInput? modifier = null;
            foreach (var held in _heldControllerInputs)
            {
                modifier = held;
                break;
            }
            _listenCallback?.Invoke(new ControllerBinding(input, modifier));
            return true;
        }

        var toRelease = new List<InputAction>();
        foreach (var action in _activeActions)
        {
            if (action.UsesControllerInput(input))
                toRelease.Add(action);
        }

        bool anyConsumed = false;
        foreach (var action in toRelease)
        {
            _activeActions.Remove(action);
            bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustReleased);
            if (!consumed && action.GameAction != null)
                InjectGameAction(action.GameAction, pressed: false);
            if (consumed)
                anyConsumed = true;
        }

        return anyConsumed;
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

    private static bool ShouldLetFocusedTextControlHandleKey(NControllerManager controller, InputEventKey keyEvent)
    {
        var focusedControl = controller.GetViewport()?.GuiGetFocusOwner() as Control;
        if (!IsTextEditingActive(focusedControl))
            return false;

        return IsTextEditingKey(keyEvent);
    }

    public static bool IsFocusedTextEditingActive()
    {
        var focusedControl = _controllerManager?.GetViewport()?.GuiGetFocusOwner() as Control;
        return IsTextEditingActive(focusedControl);
    }

    private static bool IsTextEditingActive(Control? control)
    {
        return control switch
        {
            LineEdit lineEdit => lineEdit.Editable && lineEdit.IsEditing(),
            NMegaTextEdit textEdit => textEdit.Editable && textEdit.IsEditing(),
            _ => false,
        };
    }

    private static bool IsTextEditingKey(InputEventKey keyEvent)
    {
        if (keyEvent.AltPressed)
            return false;

        if (keyEvent.Keycode is Key.Backspace or Key.Delete or Key.Left or Key.Right or Key.Home or Key.End)
            return true;

        if (keyEvent.CtrlPressed)
        {
            return keyEvent.Keycode is Key.A or Key.C or Key.V or Key.X or Key.Y or Key.Z;
        }

        return keyEvent.Unicode >= 32;
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
