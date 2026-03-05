# SayTheSpire2 - Accessibility Mod for Slay the Spire 2

## Project Overview
Accessibility mod for blind players of Slay the Spire 2. Replaces Godot's buggy built-in AccessKit screen reader with custom TTS via Windows SAPI (System.Speech.Synthesis). Named after the original STS1 mod "SayTheSpire".

## Build & Deploy
```
python build.py
```
This builds the DLL, creates the PCK, and copies everything to the game's `mods/` directory. Then restart the game to test. Use `python` not `python3`.

## Check Logs
Game logs are at: `%APPDATA%/SlayTheSpire2/logs/godot.log`
All mod log lines are prefixed with `[AccessibilityMod]`.

## Architecture

### Game Details
- **Engine**: Godot 4.5.1 custom build, C#/.NET 9.0
- **Game DLLs**: `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/` (sts2.dll, GodotSharp.dll, 0Harmony.dll)
- **Mods dir**: `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/`
- **Settings**: `%APPDATA%/SlayTheSpire2/steam/76561198124893519/settings.save` (JSON, `mod_settings.mods_enabled` must be true)
- **Decompiled game source**: `/c/Users/bradj/AppData/Local/Temp/sts2_decompiled/` (3299 .cs files)

### Mod Loading Flow
1. Game's ModManager scans `mods/` for `.pck` files, loads companion `.dll`
2. Finds `[ModInitializer]` attribute on `ModEntry`, calls `Initialize()`
3. Our Initialize registers assembly resolver, applies Harmony patches, starts TTS

### Key Files
- `ModEntry.cs` - Entry point. Registers assembly resolver for System.Speech.dll, creates Harmony instance, initializes all subsystems
- `Speech/SpeechManager.cs` - Windows SAPI TTS wrapper (speak, stop, queue, rate, volume)
- `Hooks/FocusHooks.cs` - Patches `NClickableControl.RefreshFocus()` to announce focused UI elements
- `Hooks/KeyboardNavHooks.cs` - Patches `NControllerManager.CheckForControllerInput()` to enable keyboard navigation
- `Patches/DisableBuiltinAccessibility.cs` - Subclasses game window to block WM_GETOBJECT, killing AccessKit

### Critical Technical Details

**Harmony patching rules in this codebase:**
- Use MANUAL patching (`harmony.Patch()`) not attribute-based `PatchAll` - PatchAll silently skips failures
- Never patch virtual methods on base classes (overrides won't be intercepted). Patch non-virtual chokepoints instead
- Example: patch `RefreshFocus` (private, non-virtual) not `OnFocus` (protected, virtual)
- Private methods: use `AccessTools.Method(typeof(Class), "MethodName")` to find them
- Protected properties: use `typeof(Class).GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic)`

**Assembly loading:**
- Game's AssemblyLoadContext only resolves sts2 and 0Harmony
- Custom `AssemblyLoadContext.Default.Resolving` handler loads System.Speech.dll from mods/ dir
- System.Speech types must NOT be referenced before resolver is registered (JIT resolves eagerly)
- That's why `SpeechManager.Initialize()` is called via `InitializeSpeech()` wrapper

**Input system:**
- Game has two modes: mouse mode and controller mode (`NControllerManager.IsUsingController`)
- Controller mode enables Godot focus-based navigation (d-pad moves between controls)
- Mouse mode disables focus navigation entirely
- Game removes default keyboard→action mappings from Godot's input map
- `InputEventKey.IsActionPressed("ui_down")` returns FALSE for arrow keys (game uses custom remapping via NInputManager)
- Keyboard keys arrive as both `InputEventKey` AND `InputEventAction` (after NInputManager remaps them)
- Our keyboard nav hook catches both event types to trigger focus mode

**Disabling AccessKit:**
- Godot's AccessKit is C++ engine-level, not patchable via Harmony
- Win32 window subclassing (`SetWindowLongPtrW`) intercepts `WM_GETOBJECT` and returns 0
- This tells Windows "no accessibility provider" - stops focus stealing, input interception, and built-in TTS
- Does NOT affect Windows Magnifier (pixel-based), high contrast, or sticky keys
- MUST keep `_wndProcDelegate` reference alive to prevent GC collection

### Game's UI Class Hierarchy (key classes)
- `NClickableControl` - Base for all interactive UI (buttons, cards, relics, etc.)
  - `RefreshFocus()` - private, called on hover and controller focus changes
  - `IsFocused` - protected property (private setter), true when hovered or controller-focused
  - `OnFocus()` / `OnUnfocus()` - protected virtual, called by RefreshFocus
- `NButton` - Extends NClickableControl, overrides OnFocus (plays hover SFX)
- `NControllerManager` - Singleton tracking input mode, switches between mouse/controller
- `NInputManager` - Keyboard/controller remapping, converts keys to InputEventAction
- `ActiveScreenContext` - Manages which screen is active, `FocusOnDefaultControl()` grabs focus
