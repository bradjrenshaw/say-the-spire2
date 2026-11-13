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
- **Game scenes**: the game's `.tscn` scene files are stored as plain text inside `SlayTheSpire2.pck` (e.g. `scenes/combat/fool_arrow.tscn`, `scenes/screens/blitz/blitz_screen.tscn`). `scripts/extract_game_locale.py`'s `parse_directory` reads the pck directory, so any scene can be pulled out to check node paths, default visibility, `focus_mode` and placeholder text that the decompiled C# doesn't show.
- **Decompiled game source (beta)**: `../sts2_decompiled_beta/` (~3890 .cs files, one per top-level type; `ilspycmd -p` crashes on the November beta DLL, so it was produced with a per-type ICSharpCode.Decompiler loop, single-threaded, XML docs off)

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
- The game's `TryGrabFocus` can set real Godot focus on a control that isn't visible-in-tree yet (its deferred grab re-checks validity but not visibility). `NClickableControl.RefreshFocus` then leaves `IsFocused` false, and `OnVisibilityChanged` only reacts to becoming *hidden* — becoming visible never re-runs `RefreshFocus`, so the focus hooks miss the control entirely (map nodes at act start in multiplayer). `UIManager.RunFocusWatchdog()` (called every frame from `Update`) compares Godot's `GuiGetFocusOwner()` against the announced/pending control and announces missed focus after 2 settled frames. It arms once per owner change (so logical control-less navigation doesn't retrigger it), honors `ShouldSuppressFocusAnnouncement`, and only runs in focus-nav mode. Its "already handled" check treats two controls as the same element when one contains the other — the focus hooks announce wrapper nodes (NCreature, card holders, settings sliders) while Godot actually focuses a child hitbox, and a raw reference comparison would read the same element twice.
- Disabled `NClickableControl`s have `FocusMode = None` set by the game. We patch both `SetEnabled` and `Disable` to restore `FocusMode.All` (some screens like `NBestiaryEntry._Ready` call `Disable()` directly, bypassing `SetEnabled`) and use `HasFocus()` fallback in `RefreshFocusPostfix` since `IsFocused` is never true for disabled controls.

**Speech and Messages:**
- `SpeechManager.Output` must NEVER use `interrupt: true`. User preference is to never interrupt existing speech.
- All user-facing text must go through the `Message` system. Use `Message.Localized("ui", "KEY", new { ... })` for mod-generated text, `Message.Raw()` only for game-provided text (card names, creature names, LocString results).
- Never use `Message.Raw()` with hardcoded English — that bypasses localization.
- `Message` supports `+` operator for composition and `Message.Join(separator, parts)` for custom separators.
- UIElement methods (`GetLabel`, `GetStatusString`, `GetTooltip`, `GetExtrasString`) return `Message?`, not `string?`. Call `.Resolve()` only at the final output point.
- Event `GetMessage()` returns `Message?`. EventDispatcher passes the Message directly to SpeechManager.
- Localization keys live in `Localization/eng/ui.json`. Language switches at runtime via `LocManager.SetLanguage` hook.

**Localization workflow (when adding new keys):**
- Source of truth is `Localization/eng/ui.json`. Add the key there first, then propagate to the 13 other locales (deu, esp, fra, ita, jpn, kor, pol, ptb, rus, spa, tha, tur, zhs).
- Cross-check terminology against the **game's own** localization. Extracted to `game_locale/` (gitignored) via `python scripts/extract_game_locale.py [--all]` — re-run after game updates. Reuse the game's word for game concepts (Boss, Bestiary, Block, Card, Relic, etc.) so the mod doesn't say a different thing than the sighted UI shows. Useful files: `bestiary.json`, `card_keywords.json`, `gameplay_ui.json`, `intents.json`, `main_menu_ui.json`.
- `python scripts/locale_tool.py audit [<lang>]` — per-locale breakdown of translated / eng-equiv / missing / stale / placeholder-mismatched keys. Audits every non-eng locale when run without an arg.
- `python scripts/locale_tool.py extract <lang>` — dumps the missing keys with eng source values as a JSON blob, suitable for handoff to a translator or LLM.
- `python scripts/locale_tool.py apply <lang> <path>` — merges a flat key→value JSON back into the locale. Refuses to overwrite existing translations without `--force`. Validates `{placeholder}` sets match the eng source.
- Locale files are **sparse** — a key only appears once it has a real translation. Anything missing falls back to eng at runtime via `LocalizationManager.Get`. So an eng-side rewording never strands a stale English copy in another language.
- "eng-equiv" entries flagged by audit are usually pure format strings (`"{buffer}: {item}"`, `"{passive}/{evoke}"`, etc.) where the translation is identical to English by design. Check before "fixing" them.
- After filling new keys, build (`dotnet build 2>&1 | tail -5`) to confirm the PCK regenerates with all locales.

**Views (data abstraction):**
- `Views/CardView.cs`, `Views/CreatureView.cs`, `Views/IntentView.cs`, `Views/RelicView.cs`, `Views/PotionView.cs` are the canonical wrappers over their game-model counterparts. **Anything that reads from a game model should go through the matching View.** Direct `card.X` / `creature.X` / `relic.X` access outside these files is the smell.
- Why: the game's model surface shifts between betas (e.g., `Creature.CombatState` going from concrete class to interface; `CardCmd.AddBadge` getting renamed). Centralizing reads in one View per model means a game update touches one file, not five.
- A View accessor either exposes a typed shortcut (e.g., `CardView.EnchantmentTitle` returning `string?`) for common cases, or exposes the raw nested model (e.g., `CardView.Enchantment` returning `EnchantmentModel?`) for callers that need the full surface. Both shapes are fine; pick whichever fits the call site.
- `view.DisplayedModel` / `view.Entity` is exposed for the boundary where we hand a raw model to a game API we don't own (game commands, `cardBuffer.Bind(model)`, holder methods that take `CardModel`). Use it sparingly; prefer adding a View accessor when the same access shows up in 2+ places.
- New View types should follow the existing shape: private constructor, `FromControl(Control?)` factory that walks parent chains to find the relevant game node, `FromModel(...)` factory for model-only views, and properties that read live from the underlying model (no caching).
- When introducing a new game-model type that multiple proxies/buffers/events read from, create a View **first**, then write the consumers against it.

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
- Powers the game hides from the local player (`PowerModel.IsVisible` false — in multiplayer a creature can hold one copy per player, e.g. Sandpit per target or the Fool's Impaled per applier, and each player is shown only their own) are not read. `Views/PowerView` owns the rule: `PowersAnnouncement` filters through it for every buffer and hotkey readout, and `CombatCreatureHandlers` drops power change events for hidden powers. `PowerView.ShownWhenHidden` is the exceptions list (currently `SandpitPower`, whose per-player countdown sighted players read from character positions): those are still listed in state readouts with the player named (`POWER.FOR_PLAYER`), but their change events stay suppressed.

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
- The July-31 beta renamed the `LobbyPlayer` struct to `StartRunLobbyPlayer` (and added `LoadRunLobbyPlayer`); stable keeps the old name, with identical field names (`id`, `character`, `isReady`). All reads go through `Views/LobbyPlayerView` (boxed-struct reflection), Harmony lobby handlers type the player param as `object`, and `StartRunLobby.LocalPlayer`/`.Players` are reached via `LobbyPlayerView.LocalPlayerOf/PlayersOf` because their signatures contain the divergent type.
- The November beta changed the load screens' `PlayerConnected` (`NMultiplayerLoadGameScreen`, `NDailyRunLoadScreen`, `NCustomRunLoadScreen`) from `ulong playerId` to `LoadRunLobbyPlayer player` (fields `id`, `isModded`, `isReady` — no `character`). Those handlers take `object __0` and read the id with `LobbyPlayerView.IdOf`.
- The November beta added Dread and Blitz game modes. `AscensionHelper.GetTitle/GetDescription` gained a `GameMode` parameter (stable: level only), so all ascension text goes through `UI/AscensionText.cs`, which takes the mode from `NAscensionPanel._gameMode` (beta-only field) in lobbies and `RunState.GameMode` in a run. The `GameMode` enum itself differs (stable: `Custom` = 3; beta: `Dread` = 3, `Blitz` = 4) — don't reference those members directly. Same beta: `NMapScreen.PlayerVoteDictionary` removed (use `MapSelectionSynchronizer.GetVote`, both branches) and `NMegaTextEdit` became a `Control` wrapping a `TextEdit` child instead of extending it.
- Blitz lobby (`NBlitzScreen`, beta-only, resolved by name) is a near-copy of `NCharacterSelectScreen` and reuses `CharacterSelectGameScreen` + the same `LobbyHooks` handlers (`PatchCharacterSelect`); the screen is held as a `Control` and its `Lobby` read by reflection. Its info popup (`NBlitzInfoDisclaimer`) is a child of the lobby, not an `NModalContainer` modal, so `ModalHooks` hooks its `_Ready` and routes it through the normal modal path. The info button and the popup's confirm are on `ui_confirm` (our Accept); embark is `ui_select`. The popup doesn't take focus (the lobby behind stays navigable) but is an `NHotkeyManager` blocking screen, so Embark is dead until it is dismissed; `ModalScreen` therefore treats mode disclaimers (`IsModeDisclaimer`) as real dialogs: it makes the popup's confirm button focusable (its scene sets no `focus_mode`), holds focus on it with all neighbours pointing at itself (re-grabbing each frame, since the game focuses a character after the popup opens), suppresses focus announcements for anything else, and hands focus back to the lobby control when the popup goes. It also claims both Select and Accept and presses the button itself, since Enter is confirm on the game's own keyboard layout but only Select in ours. The Blitz shop reuses `NMerchantInventory` with the regular rows hidden and free offers in `_blitzContainer` (beta-only field; `BlitzMerchantEntry` is a beta-only type read through `Views/BlitzMerchantEntryView`). `MerchantGameScreen` registers them three to a row (as the game wires their focus) and speaks `_blitzSelectionText` ("You may choose N more items") on open and on change; used/disabled offers have `FocusMode.None`. Blitz has no map screen (one fixed line of rooms per act; Proceed goes to the next). `Map/BlitzRun.IsActive` detects it by the run's map type (`BlitzActMap`, beta-only), not the `GameMode` enum. The top-bar strip's points (`NBlitzTopBarMapPoint`, by name) read through `ProxyBlitzMapPoint`: room type, current/traveled, position in the act, and the game's legend tip (visited rooms' history tips are not read). Travel goes through `NBlitzTopBarMap.TravelToMapCoord` (patched for `TravelEvent`), not `NMapScreen`. In multiplayer a Blitz Proceed is a map vote for the next room; `NMapScreen.OnPlayerVoteChanged` still fires, and `VotingHooks` turns it into "ready to proceed" (`MapVoteEvent` `RemoteProceed`/`LocalProceed`) instead of a node vote.
- Dread lobby (`NDreadScreen`, beta-only, by name) also reuses `CharacterSelectGameScreen` + `LobbyHooks.PatchCharacterSelect`, read through `Views/DreadLobbyView`. It has no info panel and no ascension panel (fixed at 10), so `ProxyCharacterButton`/`CharacterBuffer` drop starting HP/gold there and add `DreadStreakAnnouncement`: the current streak (drawn as a chain with no number; read via `AscensionHelper.GetDreadStreak(character, isMultiplayer, isRotating)`) and the best-streak records (text in `%ScoreLabels`/`%ScoreValues`, which describe the *selected* character — focusing a button selects it). The "Begin Rotating Streak" tickbox (`%RotatingTickbox`, singleplayer, hidden once a rotating streak runs) is mouse-only in the game — no hotkey, every character button's up/down neighbour is itself, and its scene sets no `focus_mode` (so `NClickableControl.Enable()` resets it to `None` each time) — so the screen keeps it at `FocusMode.All` and wires Down from the focused character to it each frame and registers a `ProxyDreadRotationTickbox`. The game's select action both clicks the focused control *and* fires the embark hotkey (hotkeys run in `_UnhandledInput`, and `NClickableControl._GuiInput` doesn't accept the event), so the screen claims `ui_select` while the tickbox is focused and calls `ForceClick()` itself. The Rotating button (`InitAsRotating`, `IsRandom` true, character `RotatingCharacter`) is labelled "Rotating, Next: X". Its info popup (`NDreadInfoDisclaimer`) goes through `ModalHooks` like Blitz's (`ModalScreen.IsModeDisclaimer`), plus the ten ascension modifiers in its `LeftList`/`RightList`. `Patches/DreadHooks` speaks the streak result (chain gains a link / breaks) from `PlayDreadWinStreakResultAnimation`.
- In a Dread run (`Views/DreadRunView`, recognised by `GameMode.ToString() == "Dread"`): the boss icon's tip is the act's Dominion (`EncounterModel.GetDominion()` on `SecondBossEncounter ?? BossEncounter`; title `ascension` `DREAD.title`) — `ProxyTopBar` reads it, after the two-boss tip in the last act (Dread is ascension 10, so that act always has two bosses); the portrait tip is the game mode line, the streak and the four rules, with no modifier list — `ProxyAscension` reads "Dread, Streak N" and its buffer follows the tip. (On the beta every mode's tip opens with "Game Mode: …", from the private `AscensionHelper.GetGameModeLine`; `AscensionText.GameModeLine` exposes it and the buffer includes it. The tip ends with the game's "Achievements & Epochs are locked for this run" line on daily and custom runs — `AscensionText.AchievementsLockedLine`; the check is `GameModeHelper.AreAchievementsAndEpochsLockedForRun` on the beta and the `GameModeExtension.AreAchievementsAndEpochsLocked` extension on stable.) Dominion choices (Knowledge Demon) use the standard choose-a-card screen. Dread on other screens needs nothing special: the game-over score line ("Dread x3") and run history's mode label are plain labels the existing screens read; run history's player icon shows the Dread icon with the ascension number, read as "Ascension 10".
- The main menu's Continue button shows the saved run's summary panel (`NMainMenu.ContinueRunInfo`, both branches) when focused; `ProxyContinueButton` reads it off the panel's labels (`%ProgressLabel`, `%AscensionLabel` — which is where "Dread"/"Blitz"/"Ascension N" appear — `%HealthLabel`, `%GoldLabel`, `%DateLabel`, or `%ErrorContainer`), naming the character by matching `%CharacterIcon`'s texture path against `CharacterModel.IconTexture`.
- The custom run screen's game mode dropdown (`%GameModeDropdown`, `NCustomGameModeDropdown`, beta-only, visible once every lobby player has Blitz or Dread unlocked; disabled for multiplayer clients) is a "Game Mode" row in `CustomRunGameScreen`, held as a plain `Control`, wired between the seed and the characters (where the game itself points the characters' Up and the dropdown's Down). Its value is the dropdown's `%Label`; while the list is open (`%DropdownContainer` visible, items under its `VBoxContainer`) the options are registered in an unnamed positional row so `EnsureFocus` doesn't pull focus away. Cancel closes an open list (via the dropdown's `%Dismisser`) instead of leaving the screen. Picking an option hides the focused item before the game's deferred grab returns focus to the dropdown, so `EnsureFocus` stands down for two frames after the list closes, and `PollGameMode` only speaks a mode change that wasn't the user's own (the host's, for clients).
- The Fool (November beta character) has two mechanics shown in a combat panel (`NFoolCombatUi`) that exists only for the local player and whose icons are mouse-hover only (their scenes set no `focus_mode`): **arrows** (`PlayerCombatState.Arrows`, slots that are in hand / stuck in an enemy / destroyed) and **melodies** (Wah/Cho/Ki, held for the turn once a card with that tag is played; `MelodyCmd.PlayedThisTurn`). All beta-only, read through `Views/ArrowsView` and `Views/MelodiesView` (both null for other players and on stable). Both appear in the resources line (`ResourcesAnnouncement`: player buffer + Ctrl+Y); arrows also have `ArrowsAnnouncement` on the local creature's focus line. Melodies are not powers (no `PowerModel`; the game derives them from the turn's play history), so the player and creature buffers list each held one with its game hover tip via `MelodiesAnnouncement`, just before the powers. `Patches/FoolHooks` announces a gained melody (`MelodyEvent`) from `NFoolCombatSymbol.Create`. Shooting/dislodging is not announced separately — the enemy gaining/losing Impaled already is.
- Lobby info panels differ per mode: the Blitz lobby hides starting gold (`InfoPanel/VBoxContainer/HpGoldSpacer/HpGold/Gold` invisible), so `ProxyCharacterButton.LobbyShowsStartingGold` gates the gold announcement; Blitz and Dread lobbies show an "Under Construction" badge (`%UnderConstruction`, singleplayer only) that `CharacterSelectGameScreen` speaks once per visit.
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
- `NControllerManager.InputType` (property; enum MouseAndKeyboard/KeyboardOnlyMode/Controller) — tracks input mode on the July-31 beta. Stable still has the old `IsUsingController` bool; `Input/InputManager.cs` resolves both and exposes `IsFocusNavActive`/`SetControllerMode`. The beta has a native keyboard-only mode with its own rebindable map (`_fKbInputMap` / `remappableKbOnlyInputs`) that the mod does not use yet.
- `NControllerManager._lastMousePosition` — saved mouse pos for mode switching
- `NInputManager._mKbInputMap` (beta; stable: `_keyboardInputMap`), `._controllerInputMap` — input rebinding. `remappableMKbInputs` (beta; stable: `remappableKeyboardInputs`) gates which inputs the settings panel offers keyboard rebinds for. `GetShortcutKey` was removed on the beta (split into `GetMKbHotkey`/`GetKbOnlyHotkey`); InputRebindHooks reads the map directly instead.
- The July-31 beta also renamed game actions: `ui_accept` split into `ui_end_turn` (E) + `ui_confirm` (Enter), and `mega_release_card` was removed. `InputManager.InjectGameAction` translates at injection time (our Accept injects both split actions on the beta; release-card is skipped there). Branch detection uses the `MegaInput` constant *fields* — NOT `InputMap.HasAction`, because several game actions (`ui_end_turn`, `mega_select_card_*`) are matched by name in event handlers without ever being registered in Godot's InputMap.

**Focus system:**
- `NClickableControl.IsFocused` (property) — focus state for RefreshFocus hook
- `NMerchantDialogue._label` — merchant dialogue text
- `NMerchantInventory._characterCardContainer`, `._colorlessCardContainer`, `._relicContainer`, `._potionContainer`, `._cardRemovalNode`, `._backButton` — shop rows (MerchantGameScreen registers slots per row for positions + structural wrap checks)

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
- `NBestiary._bestiaryList`, `._moveList`, `._selectedEntry`, `._epithet`, `._descriptionLabel` — bestiary screen state. The July beta removed the epithet/description detail panel: `_descriptionLabel` is gone (ProxyBestiaryEntry treats its absence as "no detail panel" and reads nothing), `_epithet` survives but is never populated (scene placeholder text), and the new `_dialogueLabel` is a character dialogue quote, not a description. The beta also renamed `NBestiaryActDivider` to `NBestiaryLabelDivider` (stable has no divider type at all); BestiaryGameScreen matches the divider by type name, not compile-time reference.
- `NBestiaryEntry.Entry` (property of type `BestiaryEntry`) — wraps the monster/encounter model and `roomType` qualifier (boss / elite / monster). `NBestiaryEntry.IsDiscovered` replaces the old `IsUnknown`. The old `_monsterType` field and `Monster` / `IsUnknown` / `UnderConstructionName` properties were removed in the late-May beta.
- `NBestiary._modeButton`, `._filterContainer`, `._isStatsMode`, `._currentFilter` — bestiary stats view (beta only; null on stable, feature gated on `HasStatsSupport` in BestiaryGameScreen). The July-31 beta moved the caption label off `NBestiary._modeLabel` onto the button itself (`NBestiaryModeButton._modeLabel` child node); ProxyBestiaryModeButton reads the button's child text first and falls back to the old field.
- `NBestiaryCharacterFilter` (beta-only type, resolved by name) — `kills`, `deaths`, `character` fields; `IsSelected`, `IsLocked`, `WinRate`, `BestiarySeenQuote`, `BestiaryKillQuote` properties (ProxyBestiaryCharacterFilter)
- `NModdingScreen._modRowContainer`, `._pendingChangesWarning` — settings mods menu (ModsGameScreen; types exist on both branches)
- `NModMenuRow._tickbox`, `._isSelected` — mod row state (ProxyModMenuRow)

**BaseLib (optional third-party mod — different rules):** ModConfigGameScreen / ProxyConfigSection support BaseLib's mod configuration submenu (`BaseLib.Config.UI.*`, resolved via `AccessTools.TypeByName`; the submenu is matched by type-name string in CompendiumHooks). Because BaseLib versions drift independently of the game and of us, these lookups deliberately degrade gracefully (`?`, feature skipped) instead of crash-on-rename. Row hover tips are injected into existing proxies via `UIElement.CollectAnnouncements`; BaseLib's `NConfigSlider` is handled by the generalized `ProxySlider` (reads `Slider`/`SliderValue` children). BaseLib's custom character select entries reuse the game's `NCharacterSelectButton` with a placeholder in `_character` (unlock source or Ironclad) — ProxyCharacterButton detects the `BaseLibCustomCharacterSelectEntry` metadata and reads the entry's `EntryTitle`/`EntryDescription` (locked variants when locked) via the game's `_delegate` field instead, and skips HP/gold/relic announcements and buffers for such entries. Powers implementing BaseLib's `IHasSecondAmount` (a second number on the power icon) get that value appended in parentheses via `UI/PowerSecondAmount.cs` (used by PowersAnnouncement and CreatureIntentFormatter). BaseLib has no custom resource or stance system — modded characters reuse the game's energy/stars (`CustomEnergyCounter` is visual-only), and orbs/pets/piles/rest-site options/rewards/badges all extend game types our existing proxies read.

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
