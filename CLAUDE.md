# SayTheSpire2 - Accessibility Mod for Slay the Spire 2

## Project Overview
Accessibility mod for blind players of Slay the Spire 2. Replaces Godot's buggy built-in AccessKit screen reader with custom TTS via Windows SAPI (System.Speech.Synthesis). Named after the original STS1 mod "SayTheSpire".

## Build & Deploy
```
dotnet build
```
This builds the DLL, creates the PCK, and copies everything to the game's `mods/` directory via MSBuild post-build targets. Then restart the game to test.

**Important:** Use `dotnet build` (Debug), NOT `dotnet build -c Release`. The post-build copy to the mods directory only runs in Debug configuration. Release builds the DLL but does not deploy it.

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

### Architectural Invariants

These rules were discovered through bugs. Check against them before making changes.

**Focus system:**
- Focus announcing happens ONLY in `UIManager.Update()`, called once per frame. Never announce focus from setters or hooks directly.
- `SetFocusedControl`/`SetFocusedElement` only store state + set dirty flag. No speech, no buffer updates.
- Pre-resolved elements (passed to `SetFocusedControl`) must never be downgraded by re-resolve. Only upgrade via screen registry (`ScreenManager.ResolveElement`), never fall back to `ProxyFactory.Create` over a pre-resolved proxy.
- `FocusContext` is a single global instance in `UIManager`, not per-screen. Path diffing is centralized.
- Mouse hover must not trigger focus announcements during controller mode. We suppress `CheckForMouseInput` via Harmony to prevent the game from switching back to mouse mode during controller navigation.
- Disabled `NClickableControl`s have `FocusMode = None` set by the game. We patch `SetEnabled` to restore `FocusMode.All` and use `HasFocus()` fallback in `RefreshFocusPostfix` since `IsFocused` is never true for disabled controls.

**Speech:**
- `SpeechManager.Output` must NEVER use `interrupt: true`. User preference is to never interrupt existing speech.

**Events:**
- Events with creature sources use `HasSourceFilter` on their `EventSettingsAttribute`. The `AllowCurrentPlayer/AllowOtherPlayers/AllowEnemies` flags control which sources the game provides visual feedback for — disallowed sources are silently dropped.
- Power decreased to 0: skip the Decreased event (check `power.Amount > 0`), let the Removed event handle it.
- Non-stacking powers (`StackType != Counter`) should not show numeric amounts (-1 is misleading).

**Buffers:**
- `FollowLatest` on a buffer means switching to it jumps to the last item (used by events buffer).
- `Repopulate()` preserves position. Use stable container/proxy references (not new objects each frame) to avoid path-diffing churn.

**Multiplayer:**
- All multiplayer event hooks must gate on `IsMultiplayer()` to avoid firing in singleplayer.
- Local player checks: use `LocalContext.IsMe(creature)` or `player.NetId == LocalContext.NetId`.
- Player names: `PlatformUtil.GetPlayerName(platform, netId)` with try/catch fallback.
- The `affects_gameplay: false` manifest field prevents the mod from blocking multiplayer connections.

**Harmony patching (additional):**
- `NCardHolder` extends `Control`, NOT `NClickableControl`. Focus hooks for card holders use `PatchOnFocus<T>` on specific subclasses that override `OnFocus` (NHandCardHolder, NGridCardHolder, NPreviewCardHolder).
- `NSelectedHandCardHolder` does NOT override `OnFocus`. Use `FocusEntered` signal connection instead.
- `NMerchantSlot` extends `Control`, not `NClickableControl`. Has its own `OnFocus` hook.

### Game's UI Class Hierarchy (key classes)
- `NClickableControl` - Base for all interactive UI (buttons, cards, relics, etc.)
  - `RefreshFocus()` - private, called on hover and controller focus changes
  - `IsFocused` - protected property (private setter), true when hovered or controller-focused
  - `OnFocus()` / `OnUnfocus()` - protected virtual, called by RefreshFocus
- `NButton` - Extends NClickableControl, overrides OnFocus (plays hover SFX)
- `NControllerManager` - Singleton tracking input mode, switches between mouse/controller
- `NInputManager` - Keyboard/controller remapping, converts keys to InputEventAction
- `ActiveScreenContext` - Manages which screen is active, `FocusOnDefaultControl()` grabs focus
