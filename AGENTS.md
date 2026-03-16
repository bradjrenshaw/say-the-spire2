# AI Agent Guidelines for SayTheSpire2

This file contains review rules and checklists for AI agents working on this codebase. Reference CLAUDE.md for full architectural details.

## Before Making Changes

### Understand the scope
- Read CLAUDE.md's "Architectural Invariants" section before touching focus, events, speech, or Harmony patches.
- If the change touches multiple subsystems, consider whether side effects could affect unrelated screens (the shop regression from the focus refactor is a cautionary example).

### Check if a plan is needed
- Simple bug fixes (one file, clear cause): proceed directly.
- New features, refactors, or changes to core systems (UIManager, FocusHooks, EventDispatcher, InputManager): plan first, get user approval.

## Code Review Checklist

### Focus System
- [ ] `SetFocusedControl`/`SetFocusedElement` only store state. No speech output, no buffer updates.
- [ ] `UIManager.Update()` is the only place focus announcements happen.
- [ ] Pre-resolved elements passed to `SetFocusedControl` are not overwritten by `ProxyFactory.Create` fallback. Only `ScreenManager.ResolveElement` (screen registry) can upgrade them.
- [ ] New controls that don't extend `NClickableControl` need explicit `FocusEntered` signal connections (see `NSelectedHandCardHolder`, `NMerchantSlot`, `NCrystalSphereCell` patterns).
- [ ] Disabled controls: verify `SetEnabledPostfix` keeps them focusable and `RefreshFocusPostfix`'s `HasFocus()` fallback announces them.

### Speech
- [ ] No `SpeechManager.Output` call uses `interrupt: true`.
- [ ] Focus-related announcements go through `UIManager.Update()`, not direct `SpeechManager.Output` calls.

### Harmony Patches
- [ ] Manual patching via `harmony.Patch()`, not `PatchAll`.
- [ ] Target method is non-virtual and declared on the target type (not inherited from a base class).
- [ ] Patch has error logging (try/catch in the postfix/prefix, or `PatchIfFound` pattern with log on failure).
- [ ] New patches are registered in the appropriate `Initialize(Harmony)` method, not scattered.
- [ ] `NCardHolder` subclass hooks: only patch those that override `OnFocus`. Check the decompiled source first.

### Events
- [ ] New event classes have `[EventSettings]` attribute and are registered in `EventRegistry.RegisterDefaults()`.
- [ ] Events with creature sources set `Source = creature` in constructor and use `hasSourceFilter: true`.
- [ ] `AllowCurrentPlayer`/`AllowOtherPlayers`/`AllowEnemies` flags match what the game visually shows to other players. Don't announce information the game doesn't display.
- [ ] Power events: Decreased handler skips when `power.Amount <= 0` (Removed event handles it).
- [ ] Non-stacking powers (`StackType != Counter`): don't show numeric amounts.

### Multiplayer
- [ ] All multiplayer-specific hooks gate on `IsMultiplayer()`.
- [ ] Player identification uses `PlatformUtil.GetPlayerName(platform, netId)` with try/catch fallback.
- [ ] Local player checks use `LocalContext.IsMe(creature)` or `player.NetId == LocalContext.NetId`.
- [ ] Orb detection, card pile subscriptions, and similar per-player logic use `LocalContext.IsMe` to filter to the local player.

### Buffers
- [ ] Stable container/proxy references for screens with `OnUpdate()` rebuilds (don't create new `ListContainer`/`ProxyCard` objects each frame).
- [ ] `FollowLatest` buffers jump to last item on switch.
- [ ] `AlwaysEnabledBuffers` override on screens that need buffers to persist across focus changes (e.g., lobby buffer on character select).

### Grid/Position
- [ ] Grid coordinates use `(x, y)` cartesian order (column first, row second).
- [ ] Verify focus neighbor wiring matches the visual/navigation order. Check if the game's card order is reversed.

### Proxy Elements
- [ ] `ProxyFactory.Create` handles the new control type, or the caller provides a pre-resolved proxy.
- [ ] `GetLabel()`, `GetStatusString()`, `GetTooltip()` handle null gracefully (the control or its children may not exist).
- [ ] Star costs use `GetStarCostWithModifiers()` not `CurrentStarCost` (to reflect Void Form and similar modifiers).

### Installer (Rust)
- [ ] Version comparison uses `semver` crate, handles both `v` and `V` prefixes.
- [ ] New installation config fields are added to `InstallationConfig` struct, `read_installation_config`, and `save_installation_config`.
- [ ] Settings file retry dialog: `enable_mods_with_retry` handles missing settings.save.
- [ ] `MOD_FILES` in `paths.rs` is updated when new files are added to the mod distribution.

## Known Fragile Areas

These areas have historically broken due to unintended side effects. Test them after changes to core systems:

1. **Shop (merchant slots)** — ProxyMerchantSlot relies on pre-resolved elements. Any change to UIManager.Update re-resolve logic can break it.
2. **Settings navigation** — Entering/exiting subcategories depends on FocusContext path diffing and NavigableContainer.FocusFirst.
3. **Hand select screen** — Selected card row uses FocusEntered signals, stable proxy cache, and per-frame OnUpdate rebuild.
4. **Crystal sphere grid** — Custom FocusEntered signals, grid coordinates, and per-frame focus neighbor wiring.
5. **Disabled buttons** — SetEnabled postfix + HasFocus fallback in RefreshFocusPostfix. Breaking either silently hides locked content.
6. **Card reward screens** — Card holder order may be reversed from scene tree order. Check position announcements.
7. **Multiplayer lobby** — LobbyBuffer binding, player list, embark/unready state transitions.
8. **Mouse hover during controller mode** — CheckForMouseInput must be suppressed to prevent focus stealing.

## Commit Guidelines

- Commit messages should describe the "why" not just the "what".
- If a fix addresses a regression, mention what caused the regression.
- Include `Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>` on AI-assisted commits.
- Don't amend previous commits — create new ones.
- Don't push to remote unless the user explicitly asks.
