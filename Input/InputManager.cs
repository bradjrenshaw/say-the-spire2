using System.Collections.Generic;
using System.Linq;
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

    /// <summary>Look up a registered action by its key, or null if unknown.</summary>
    public static InputAction? FindAction(string key) =>
        _actions.FirstOrDefault(a => a.Key == key);

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
        AccessTools.Field(typeof(NInputSettingsPanel), "_listeningEntry");

    private static readonly PropertyInfo IsUsingControllerProp =
        AccessTools.Property(typeof(NControllerManager), "IsUsingController")!;

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
        _actions.Add(new InputAction("ui_accept", "Accept", gameAction: "ui_accept", localizationKey: "INPUT.ACCEPT")
            .AddBinding(Key.E)
            .AddBinding(ControllerInput.Y));
        _actions.Add(new InputAction("ui_select", "Select", gameAction: "ui_select", localizationKey: "INPUT.SELECT")
            .AddBinding(Key.Enter)
            .AddBinding(ControllerInput.A));
        _actions.Add(new InputAction("ui_cancel", "Cancel", gameAction: "ui_cancel", localizationKey: "INPUT.CANCEL")
            .AddBinding(Key.Backspace)
            .AddBinding(ControllerInput.B));
        _actions.Add(new InputAction("ui_up", "Navigate Up", gameAction: "ui_up", localizationKey: "INPUT.NAVIGATE_UP")
            .AddBinding(Key.Up)
            .AddBinding(ControllerInput.DpadUp)
            .AddBinding(ControllerInput.LeftStickUp));
        _actions.Add(new InputAction("ui_down", "Navigate Down", gameAction: "ui_down", localizationKey: "INPUT.NAVIGATE_DOWN")
            .AddBinding(Key.Down)
            .AddBinding(ControllerInput.DpadDown)
            .AddBinding(ControllerInput.LeftStickDown));
        _actions.Add(new InputAction("ui_left", "Navigate Left", gameAction: "ui_left", localizationKey: "INPUT.NAVIGATE_LEFT")
            .AddBinding(Key.Left)
            .AddBinding(ControllerInput.DpadLeft)
            .AddBinding(ControllerInput.LeftStickLeft));
        _actions.Add(new InputAction("ui_right", "Navigate Right", gameAction: "ui_right", localizationKey: "INPUT.NAVIGATE_RIGHT")
            .AddBinding(Key.Right)
            .AddBinding(ControllerInput.DpadRight)
            .AddBinding(ControllerInput.LeftStickRight));
        _actions.Add(new InputAction("mega_peek", "Peek", gameAction: "mega_peek", localizationKey: "INPUT.PEEK")
            .AddBinding(Key.Space)
            .AddBinding(ControllerInput.LeftStickClick));
        _actions.Add(new InputAction("mega_view_draw_pile", "View Draw Pile", gameAction: "mega_view_draw_pile", localizationKey: "INPUT.VIEW_DRAW_PILE")
            .AddBinding(Key.A)
            .AddBinding(ControllerInput.LeftShoulder, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("mega_view_discard_pile", "View Discard Pile", gameAction: "mega_view_discard_pile", localizationKey: "INPUT.VIEW_DISCARD_PILE")
            .AddBinding(Key.S)
            .AddBinding(ControllerInput.RightShoulder, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("mega_view_deck_and_tab_left", "View Deck / Tab Left", gameAction: "mega_view_deck_and_tab_left", localizationKey: "INPUT.VIEW_DECK_TAB_LEFT")
            .AddBinding(Key.D)
            .AddBinding(ControllerInput.LeftShoulder));
        _actions.Add(new InputAction("mega_view_exhaust_pile_and_tab_right", "View Exhaust / Tab Right", gameAction: "mega_view_exhaust_pile_and_tab_right", localizationKey: "INPUT.VIEW_EXHAUST_TAB_RIGHT")
            .AddBinding(Key.F)
            .AddBinding(ControllerInput.RightShoulder));
        _actions.Add(new InputAction("mega_view_map", "View Map", gameAction: "mega_view_map", localizationKey: "INPUT.VIEW_MAP")
            .AddBinding(Key.M)
            .AddBinding(ControllerInput.Back));
        _actions.Add(new InputAction("mega_pause_and_back", "Pause / Back", gameAction: "mega_pause_and_back", localizationKey: "INPUT.PAUSE_BACK")
            .AddBinding(Key.Escape)
            .AddBinding(ControllerInput.Start));
        _actions.Add(new InputAction("mega_top_panel", "Top Panel", gameAction: "mega_top_panel", localizationKey: "INPUT.TOP_PANEL")
            .AddBinding(Key.T)
            .AddBinding(ControllerInput.X));
        // Numbered series: prefix localized once at registration, suffix is the
        // bare index (universal across languages). Not runtime-dynamic but the
        // simpler path; saves 11 loc keys per language.
        var creatureStatus = Ui("INPUT.CREATURE_STATUS", "Creature Status");
        var statusKeys = new[] { Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5, Key.Key6,
            Key.Key7, Key.Key8, Key.Key9, Key.Key0, Key.Minus, Key.Equal };
        for (int i = 0; i < statusKeys.Length; i++)
        {
            int n = i + 1;
            // Only the first 10 have a corresponding game action; 11 and 12 are mod-only.
            var gameAction = n <= 10 ? $"mega_select_card_{n}" : null;
            _actions.Add(new InputAction($"mega_select_card_{n}", $"{creatureStatus} {n}", gameAction: gameAction).AddBinding(statusKeys[i]));
        }
        _actions.Add(new InputAction("mega_release_card", "Release Card", gameAction: "mega_release_card", localizationKey: "INPUT.RELEASE_CARD").AddBinding(Key.Down));
    }

    private static void RegisterModActions()
    {
        _actions.Add(new InputAction("nav_home", "Jump to First Element", localizationKey: "UI_NAV.HOME")
            .AddBinding(Key.Home));
        _actions.Add(new InputAction("nav_end", "Jump to Last Element", localizationKey: "UI_NAV.END")
            .AddBinding(Key.End));
        _actions.Add(new InputAction("buffer_next_item", "Buffer Next Item", localizationKey: "INPUT.NEXT_BUFFER_ITEM").AddBinding(Key.Up, ctrl: true)
            .AddBinding(ControllerInput.RightStickUp));
        _actions.Add(new InputAction("buffer_prev_item", "Buffer Previous Item", localizationKey: "INPUT.PREV_BUFFER_ITEM").AddBinding(Key.Down, ctrl: true)
            .AddBinding(ControllerInput.RightStickDown));
        _actions.Add(new InputAction("buffer_next", "Next Buffer", localizationKey: "INPUT.NEXT_BUFFER").AddBinding(Key.Right, ctrl: true)
            .AddBinding(ControllerInput.RightStickRight));
        _actions.Add(new InputAction("buffer_prev", "Previous Buffer", localizationKey: "INPUT.PREV_BUFFER").AddBinding(Key.Left, ctrl: true)
            .AddBinding(ControllerInput.RightStickLeft));
        _actions.Add(new InputAction("map_poi_prev", "Previous Point of Interest", localizationKey: "MAP_POI.ACTION_PREVIOUS")
            .AddBinding(Key.Comma)
            .AddBinding(ControllerInput.RightStickLeft, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_poi_next", "Next Point of Interest", localizationKey: "MAP_POI.ACTION_NEXT")
            .AddBinding(Key.Period)
            .AddBinding(ControllerInput.RightStickRight, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_poi_toggle_mode", "Toggle Point of Interest Mode", localizationKey: "MAP_POI.ACTION_TOGGLE_MODE")
            .AddBinding(Key.Backslash)
            .AddBinding(ControllerInput.RightStickUp, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("map_toggle_current_marker", "Toggle Current Marker", localizationKey: "MAP_MARKERS.ACTION_TOGGLE_CURRENT")
            .AddBinding(Key.Slash));
        _actions.Add(new InputAction("map_clear_all_markers", "Clear All Markers", localizationKey: "MAP_MARKERS.ACTION_CLEAR_ALL")
            .AddBinding(Key.Slash, ctrl: true, shift: true));
        _actions.Add(new InputAction("dev_console", "Dev Console", localizationKey: "INPUT.DEV_CONSOLE").AddBinding(Key.Quoteleft));
        _actions.Add(new InputAction("feedback", "Report Issue", localizationKey: "INPUT.FEEDBACK").AddBinding(Key.F2));
        _actions.Add(new InputAction("help", "Help", localizationKey: "INPUT.HELP").AddBinding(Key.F1)
            .AddBinding(ControllerInput.Back, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("reset_bindings", "Reset Bindings", localizationKey: "INPUT.RESET_BINDINGS").AddBinding(Key.R, ctrl: true, shift: true));
        _actions.Add(new InputAction("announce_gold", "Announce Gold", localizationKey: "INPUT.ANNOUNCE_GOLD").AddBinding(Key.G, ctrl: true)
            .AddBinding(ControllerInput.A, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_hp", "Announce HP", localizationKey: "INPUT.ANNOUNCE_HP").AddBinding(Key.H, ctrl: true)
            .AddBinding(ControllerInput.A, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_block", "Announce Block", localizationKey: "INPUT.ANNOUNCE_BLOCK").AddBinding(Key.B, ctrl: true)
            .AddBinding(ControllerInput.B, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_energy", "Announce Energy", localizationKey: "INPUT.ANNOUNCE_ENERGY").AddBinding(Key.Y, ctrl: true)
            .AddBinding(ControllerInput.X, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_powers", "Announce Powers", localizationKey: "INPUT.ANNOUNCE_POWERS").AddBinding(Key.P, ctrl: true)
            .AddBinding(ControllerInput.Y, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_intents", "Announce Intents", localizationKey: "INPUT.ANNOUNCE_INTENTS").AddBinding(Key.I, ctrl: true)
            .AddBinding(ControllerInput.Y, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_summarized_intents", "Announce Summarized Intents", localizationKey: "INPUT.ANNOUNCE_SUMMARIZED_INTENTS").AddBinding(Key.I, alt: true)
            .AddBinding(ControllerInput.X, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_boss", "Announce Boss", localizationKey: "INPUT.ANNOUNCE_BOSS").AddBinding(Key.N, ctrl: true)
            .AddBinding(ControllerInput.B, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("announce_relic_counters", "Announce Relic Counters", localizationKey: "INPUT.ANNOUNCE_RELIC_COUNTERS").AddBinding(Key.R, ctrl: true)
            .AddBinding(ControllerInput.Back, modifier: ControllerInput.RightTrigger));
        _actions.Add(new InputAction("mod_settings", "Mod Settings", localizationKey: "INPUT.MOD_SETTINGS").AddBinding(Key.M, ctrl: true)
            .AddBinding(ControllerInput.Start, modifier: ControllerInput.LeftTrigger));
        var combatantIntent = Ui("INPUT.COMBATANT_INTENT", "Combatant Intent");
        var intentKeys = new[] { Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5, Key.Key6,
            Key.Key7, Key.Key8, Key.Key9, Key.Key0, Key.Minus, Key.Equal };
        for (int i = 0; i < intentKeys.Length; i++)
        {
            int n = i + 1;
            _actions.Add(new InputAction($"announce_combatant_intent_{n}", $"{combatantIntent} {n}").AddBinding(intentKeys[i], shift: true));
        }

        // Alt + number reads a combatant's powers in the same format Ctrl+P uses
        // for the player. Same key per index as Combatant Status (plain number)
        // and Combatant Intent (Shift+number), so index N is the same combatant
        // across all three.
        var combatantPowers = Ui("INPUT.COMBATANT_POWERS", "Combatant Powers");
        var powersKeys = new[] { Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5, Key.Key6,
            Key.Key7, Key.Key8, Key.Key9, Key.Key0, Key.Minus, Key.Equal };
        for (int i = 0; i < powersKeys.Length; i++)
        {
            int n = i + 1;
            _actions.Add(new InputAction($"announce_combatant_powers_{n}", $"{combatantPowers} {n}").AddBinding(powersKeys[i], alt: true));
        }
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
        if (!InterceptInput || IsGameListeningForRebind() || IsDevConsoleVisible())
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
                    else if (action.GameAction != null && !IsFocusedControlLockedClick(action))
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
            if (!consumed && action.GameAction != null && !IsFocusedControlLockedClick(action))
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
                    EnsureFocusMode(action);
                    if (!anyConsumed)
                    {
                        bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustPressed);
                        if (consumed)
                            anyConsumed = true;
                        else if (action.GameAction != null && !IsFocusedControlLockedClick(action))
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
            EnsureFocusMode(unmatchedFallback);
            if (!anyConsumed)
            {
                bool consumed = ScreenManager.DispatchAction(unmatchedFallback, InputActionState.JustPressed);
                if (consumed)
                    anyConsumed = true;
                else if (unmatchedFallback.GameAction != null && !IsFocusedControlLockedClick(unmatchedFallback))
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
            if (!consumed && action.GameAction != null && !IsFocusedControlLockedClick(action))
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

    public static bool IsDevConsoleVisible()
    {
        try { return MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole.Instance.Visible; }
        catch (System.Exception e) { Log.Info($"[AccessibilityMod] DevConsole visibility check failed: {e.Message}"); return false; }
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
            // Warp mouse offscreen so it doesn't interfere with controller-style focus navigation
            const float MouseHideOffset = -1000f;
            viewport.WarpMouse(Vector2.One * MouseHideOffset);
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

    /// <summary>
    /// Activate-style actions (ui_accept / ui_select) on a Godot-disabled
    /// NClickableControl. The game uses FocusMode = None to block clicks on
    /// locked controls; we restore FocusMode.All so screen readers can
    /// announce them, but must reinstate the click-block here so locked
    /// buttons whose Released signal doesn't gate on IsEnabled (e.g. the
    /// Daily / Custom run buttons in NSingleplayerSubmenu) don't fire.
    /// </summary>
    private static bool IsFocusedControlLockedClick(InputAction action)
    {
        if (action.Key != "ui_accept" && action.Key != "ui_select")
            return false;
        var focused = _controllerManager?.GetViewport()?.GuiGetFocusOwner();
        return focused is NClickableControl c && !c.IsEnabled;
    }
}
