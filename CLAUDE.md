# SayTheSpire2 - Accessibility Mod for Slay the Spire 2

## Project Overview
Accessibility mod for blind players of Slay the Spire 2. Replaces Godot's buggy built-in AccessKit screen reader with custom TTS via Windows SAPI (System.Speech.Synthesis). Named after the original STS1 mod "SayTheSpire".

## Build & Deploy
```
dotnet build
```
This builds the DLL, creates the PCK, and copies everything to the game's `mods/` directory via MSBuild post-build targets. Then restart the game to test.

**Important:** Use `dotnet build` (Debug), NOT `dotnet build -c Release`. The post-build copy to the mods directory only runs in Debug configuration. Release builds the DLL but does not deploy it.

**Verifying builds:** Warnings print asynchronously after the initial output. Always use `dotnet build 2>&1 | tail -5` to capture the final summary with the warning/error count. Never use `grep` to check for warnings — it may miss them.

## Check Logs
Game logs are at: `%APPDATA%/SlayTheSpire2/logs/godot.log`
All mod log lines are prefixed with `[AccessibilityMod]`.

## Architecture

### Game Details
- **Engine**: Godot 4.5.1 custom build, C#/.NET 9.0
- **Game DLLs**: `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/` (sts2.dll, GodotSharp.dll, 0Harmony.dll)
- **Mods dir**: `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/`
- **Settings**: `%APPDATA%/SlayTheSpire2/steam/76561198124893519/settings.save` (JSON, `mod_settings.mods_enabled` must be true)
- **Decompiled game source (stable)**: `../sts2_decompiled_stable/` (~3304 .cs files)
- **Decompiled game source (beta)**: `../sts2_decompiled_beta/` (~3299 .cs files)

### Mod Loading Flow
1. Game's ModManager scans `mods/` for `.pck` files, loads companion `.dll`
2. Finds `[ModInitializer]` attribute on `ModEntry`, calls `Initialize()`
3. Our Initialize registers assembly resolver, applies Harmony patches, starts TTS

### Key Files
- `ModEntry.cs` - Entry point. Registers assembly resolver for System.Speech.dll, creates Harmony instance, initializes all subsystems, registers settings and keybinding categories
- `Speech/SpeechManager.cs` - Windows SAPI TTS wrapper (speak, stop, queue, rate, volume)
- `Patches/FocusHooks.cs` - Patches `NClickableControl.RefreshFocus()` to announce focused UI elements
- `Patches/KeyboardNavHooks.cs` - Patches `NControllerManager.CheckForControllerInput()` to enable keyboard navigation
- `Patches/DisableBuiltinAccessibility.cs` - Subclasses game window to block WM_GETOBJECT, killing AccessKit
- `Localization/Message.cs` - Composable message system for localized speech output
- `Localization/LocalizationManager.cs` - JSON-based localization with language hot-switching
- `Help/HelpMessage.cs` - Data model for context-sensitive help system
- `Help/HelpScreenBuilder.cs` - Collects help from screen stack with dedup
- `UI/Screens/HelpScreen.cs` - F1 help overlay with browsable controls list
- `UI/Screens/ModalScreen.cs` - Screen wrapper for game modal dialogs
- `UI/Screens/RewardsGameScreen.cs` - Post-combat rewards screen with position info
- `UI/Screens/GameScreen.cs` - Base class for static-layout screens. Provides shared utilities: `ConnectFocusSignal`, `Activate`, `IsUsable`, `IsVisible`, `GetButtonStatus`, and `_connectedControls` field
- `UI/CardGridReflection.cs` - Centralized reflection for `NCardGrid._cardRows` and `.Columns`
- `Multiplayer/MultiplayerHelper.cs` - Shared multiplayer utilities: `GetPlayerName`, `GetCreatureName`, `GetPlayerDisplayName`, `IsMultiplayer`, `IsLocalPlayer`
- `UI/ResourceHelper.cs` - Centralized energy/stars resource string formatting
- `Patches/HarmonyHelper.cs` - `PatchIfFound()` with method validation, try/catch, and optional `parameterTypes`

### Critical Technical Details

**Harmony patching rules in this codebase:**
- Use MANUAL patching via `HarmonyHelper.PatchIfFound()` — it validates the method exists, logs success/failure, and catches patch exceptions. Call it directly with `typeof(HandlerClass)`: `HarmonyHelper.PatchIfFound(harmony, typeof(Target), "Method", typeof(MyHooks), nameof(MyPostfix), "Label")`
- Do NOT use attribute-based `PatchAll` — it silently skips failures
- Do NOT create local `PatchIfFound` wrapper methods in hook files — call `HarmonyHelper.PatchIfFound` directly
- Never patch virtual methods on base classes (overrides won't be intercepted). Patch non-virtual chokepoints instead
- Example: patch `RefreshFocus` (private, non-virtual) not `OnFocus` (protected, virtual)
- All reflection lookups use `AccessTools.Field/Property/Method` (not `typeof().GetField` with `BindingFlags`)

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

**Speech and Messages:**
- `SpeechManager.Output` must NEVER use `interrupt: true`. User preference is to never interrupt existing speech.
- All user-facing text must go through the `Message` system. Use `Message.Localized("ui", "KEY", new { ... })` for mod-generated text, `Message.Raw()` only for game-provided text (card names, creature names, LocString results).
- Never use `Message.Raw()` with hardcoded English — that bypasses localization.
- `Message` supports `+` operator for composition and `Message.Join(separator, parts)` for custom separators.
- UIElement methods (`GetLabel`, `GetStatusString`, `GetTooltip`, `GetExtrasString`) return `Message?`, not `string?`. Call `.Resolve()` only at the final output point.
- Event `GetMessage()` returns `Message?`. EventDispatcher passes the Message directly to SpeechManager.
- Localization keys live in `Localization/eng/ui.json`. Language switches at runtime via `LocManager.SetLanguage` hook.

**Help system:**
- `Screen.GetHelpMessages()` returns contextual help. `HelpScreenBuilder` walks screens deepest-first, deduplicating controls by action key.
- `HelpMessage.Exclusive` flag: exclusive messages only show when their screen is the innermost active screen.
- `ControlHelpMessage` supports multiple action keys (`ActionKeys` list) for grouped controls (e.g., "Select Combatant 1-12").
- `HelpScreen` uses `ClaimAllActions()` to block all input. Self-closes on `OnUnfocus()` if another screen pushes on top.
- `Screen.ClaimAllActions()` sets a flag so `HasClaimed()` returns true for everything. `ShouldPropagate` still defaults to false.

**Events:**
- Events with creature sources use `HasSourceFilter` on their `EventSettingsAttribute`. The `AllowCurrentPlayer/AllowOtherPlayers/AllowEnemies` flags control which sources the game provides visual feedback for — disallowed sources are silently dropped.
- Event types are auto-discovered via assembly scanning for `[EventSettings]` attribute — no manual registration needed in `EventRegistry.RegisterDefaults()`.
- Events can add custom sub-settings by implementing a static `RegisterSettings(CategorySetting)` method (see `TurnEvent`, `EnemyMoveEvent`).
- Power decreased to 0: skip the Decreased event (check `power.Amount > 0`), let the Removed event handle it.
- Non-stacking powers (`StackType != Counter`) should not show numeric amounts (-1 is misleading).

**Buffers:**
- `FollowLatest` on a buffer means switching to it jumps to the last item (used by events buffer).
- `Repopulate()` preserves position. Use stable container/proxy references (not new objects each frame) to avoid path-diffing churn.

**Modals:**
- Game modals push a `ModalScreen` via `ModalHooks.AddPostfix`. Removed on `NModalContainer.Clear` or when the modal node is freed (safety check in `OnUpdate`).
- `NCombatRulesFtue` (multi-page tutorial) gets tutorial-specific help messages instead of generic confirm/cancel.
- Modal buttons are registered as elements with `FocusEntered` signals for proper proxy resolution.

**Rewards:**
- `RewardsGameScreen` is pushed via `OverlayHooks` when `NRewardsScreen` opens. Provides position info and help for post-combat rewards.
- State token polling rebuilds when rewards change (e.g., after claiming one).
- `ProxyRewardButton.GetTypeKey()` returns "potion"/"relic"/"card" based on reward type. Delegates tooltip/status to inner proxies.

**Error handling:**
- Never use empty `catch { }` blocks. Every catch must log the exception with `Log.Error` or `Log.Info`.
- Fallback-style catches (e.g., try a modifier, fall back to base value) should log at `Log.Info` level.
- Errors indicating broken functionality should log at `Log.Error` level.

**Null safety and warnings:**
- The build must have 0 warnings. Do not suppress warnings with `#pragma` or `[SuppressMessage]`.
- Reflection lookups on game internals (`AccessTools.Field/Property/Method(...)`) use `!` intentionally — a crash on a renamed target is preferred over silent degradation that misleads blind users. This applies to new code too, not just existing code.
- For all other nullable references (node lookups, Godot queries like `GetNodeOrNull`, game model properties, method results that can legitimately return null), use `?.`, early returns, or `if (x is Type t)` pattern matching. Do not reach for `!` to silence a warning.
- Prefer `if (x is Type t)` pattern matching over `(Type)x` casts for safer type narrowing.

**Multiplayer:**
- All multiplayer event hooks must gate on `IsMultiplayer()` to avoid firing in singleplayer. This includes voting hooks (`TravelToMapCoord`, `MapPointSelectedLocally`, etc.).
- Local player checks: use `LocalContext.IsMe(creature)` or `player.NetId == LocalContext.NetId`.
- Player names: use `MultiplayerHelper.GetPlayerDisplayName(player)` for the creature-first-then-netid pattern. Use `GetPlayerName`/`GetCreatureName` for specific needs.
- The `affects_gameplay: false` manifest field prevents the mod from blocking multiplayer connections.

**Harmony patching (additional):**
- `NCardHolder` extends `Control`, NOT `NClickableControl`. Focus hooks for card holders use `PatchOnFocus<T>` on specific subclasses that override `OnFocus` (NHandCardHolder, NGridCardHolder, NPreviewCardHolder).
- `NSelectedHandCardHolder` does NOT override `OnFocus`. Use `FocusEntered` signal connection instead.
- `NMerchantSlot` extends `Control`, not `NClickableControl`. Has its own `OnFocus` hook.

### Critical Reflection Targets

These private fields/properties are accessed via reflection. A game update renaming them will break the mod silently (the field resolves to null, and features degrade). Check these after game updates:

**Input system:**
- `NInputSettingsPanel._listeningEntry` — detects when game is rebinding keys
- `NControllerManager.IsUsingController` (property) — tracks input mode
- `NControllerManager._lastMousePosition` — saved mouse pos for mode switching
- `NInputManager._keyboardInputMap`, `._controllerInputMap` — input rebinding

**Focus system:**
- `NClickableControl.IsFocused` (property) — focus state for RefreshFocus hook
- `NMerchantDialogue._label` — merchant dialogue text

**Map:**
- `NMapScreen._mapPointDictionary` — coord-to-NMapPoint lookup for voting
- `NMapScreen._map`, `._runState` — map data access

**Events:**
- `NEventLayout._title`, `._event` — event title and model
- `NAncientEventLayout._dialogueContainer` — ancient event dialogue
- `NTreasureRoomRelicCollection._isEmptyChest` — empty chest detection

**Combat:**
- `NCardGrid._cardRows`, `.Columns` — card grid layout (use `CardGridReflection.cs`, not local declarations)
- `NSimpleCardSelectScreen._selectedCards` — selected cards in grid selection
- `AbstractIntent.IntentTitle` (property) — creature intent name
- `NChooseABundleSelectionScreen._bundlePreviewCards`, `._bundleRow` — bundle preview focus wiring

**UI elements:**
- `NSettingsSlider._slider` — slider value access
- `RelicReward._relic` — reward relic model
- `NTopBarHp._player`, `NTopBarGold._player` — player reference for HP/gold
- `NTopBarRoomIcon._runState`, `NTopBarFloorIcon._runState`, `NTopBarBossIcon._runState` — run state
- `NDeckHistoryEntry._amount` — card count in deck history
- `NLabPotionHolder._model`, `._visibility` — potion lab holder state
- `NRunHistoryPlayerIcon._ascensionLabel`, `._achievementLock`, `._hoverTips` — run history player icon
- `NMapPointHistoryEntry._entry`, `._questIcon`, `._player` — run history map point
- `NDropdownPositioner._dropdownNode` — settings dropdown positioning

**Screens:**
- `NCrystalSphereScreen._cellContainer`, `._entity` — crystal sphere grid
- `NGameOverScreen._score`, `._encounterQuote` — game over display
- `NTimelineScreen._epochSlotContainer` — timeline slots
- `NCharacterSelectButton.IsSelected` (property), `._isSelected` — character selection state
- `NDailyRunScreen._lobby`, `NDailyRunLoadScreen._lobby`, `NCustomRunLoadScreen._lobby` — lobby access
- `NMultiplayerLoadGameScreen._runLobby` — multiplayer load game lobby
- `NRunHistory.SelectPlayer` (method) — run history player selection

**Daily leaderboard (DailyLeaderboardAdapter.cs):**
- `NDailyRunLeaderboard._scoreContainer`, `._loadingIndicator`, `._noScoresIndicator`, `._noFriendsIndicator`, `._noScoreUploadIndicator` — leaderboard state indicators
- `NDailyRunLeaderboard._currentPage`, `._leftArrow`, `._rightArrow`, `._paginator` — pagination controls
- `NDailyRunLeaderboard.SetPage` (method) — page navigation
- `NDailyRunLeaderboardRow._isHeader` — row type detection
- `NLeaderboardDayPaginator._label`, `._leftArrow`, `._rightArrow` — day paginator UI
- `NLeaderboardDayPaginator.PageLeft`, `.PageRight` (methods) — day navigation

### Game's UI Class Hierarchy (key classes)
- `NClickableControl` - Base for all interactive UI (buttons, cards, relics, etc.)
  - `RefreshFocus()` - private, called on hover and controller focus changes
  - `IsFocused` - protected property (private setter), true when hovered or controller-focused
  - `OnFocus()` / `OnUnfocus()` - protected virtual, called by RefreshFocus
- `NButton` - Extends NClickableControl, overrides OnFocus (plays hover SFX)
- `NControllerManager` - Singleton tracking input mode, switches between mouse/controller
- `NInputManager` - Keyboard/controller remapping, converts keys to InputEventAction
- `ActiveScreenContext` - Manages which screen is active, `FocusOnDefaultControl()` grabs focus
