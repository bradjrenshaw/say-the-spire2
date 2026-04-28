Perform a full codebase audit of the SayTheSpire2 mod.

## Your Role
You are performing a comprehensive audit of the entire codebase, not just a diff. Focus on architectural health, abstraction quality, and long-term maintainability.

## Instructions

1. Read CLAUDE.md and AGENTS.md for project context and invariants.
2. Scan all .cs files in the project using Glob and Grep.
3. Analyze each area below and report findings.

## Audit Areas

### Architecture & Abstractions

**Missed abstractions:**
- Are there patterns repeated across 3+ files that should be a shared base class or utility?
- Are there switch statements or type checks that should be polymorphism?
- Are there string-based lookups that should be typed (enums, constants, generics)?

**Over-abstraction:**
- Are there base classes or interfaces used by only one implementation?
- Are there layers of indirection that don't add value?
- Are there generics or patterns that make the code harder to follow without benefit?

**Inconsistent abstractions:**
- Do similar subsystems use different patterns for the same problem? (e.g., some screens use GameScreen, others don't; some hooks use PatchIfFound, others inline the patching)
- Are there older files that haven't been updated to use newer patterns?

**View abstraction (data wrappers over game models):**
- Game-model reads (CardModel, Creature, RelicModel, PotionModel, AbstractIntent, etc.) should go through their matching `Views/*View.cs` wrapper. Direct property access on the raw model outside the View file is the warning sign.
- Scan proxies, buffers, events, screens, and patches for direct `card.X` / `creature.X` / `relic.X` / `potion.X` / `model.X` access. Each one is a candidate for migration to the matching View.
- Exceptions where raw-model access is correct: handing the model to a game API we don't own (`cardBuffer.Bind(view.DisplayedModel)`, `intent.GetHoverTip(...)`, `holder.CardModel = ...`). Flag the exception explicitly with a comment when it's not obvious.
- Missing accessors: if the same `model.X` appears in 2+ files outside the View, the View is missing that accessor — adding it is the fix.
- Missing Views: if a game-model type is read from in 2+ places and there's no matching `*View.cs`, that's a candidate for a new View.
- Why this matters: when the game shifts a model's surface between betas (which it does regularly), having one file to update prevents "fixed it in three places, missed the fourth" bugs.

### Localization & Message Usage

**Message.Raw() misuse:**
- Scan for `Message.Raw()` calls containing hardcoded English text (not game-provided). These should be `Message.Localized()`.
- Scan for `Message.Raw(LocalizationManager.GetOrDefault(...))` — redundant pattern, should be `Message.Localized()` directly.
- Scan for resolve-then-wrap anti-pattern: `Message.Localized(...).Resolve()` fed into `string.Join` / interpolation / `+`, then passed to `Message.Raw(...)` at the end. This collapses the pieces into a frozen string so subsequent language switches can't re-translate. Compose with `Message.Join(separator, parts)`, the `+` operator (space-joined), or `Message.Sep(", ")` between `+` ops instead.
- Check that all Event `GetMessage()` implementations use `Message.Localized()` for format templates.

**`.Resolve()` may only appear at output boundaries:**
`.Resolve()` collapses a `Message` into a string and freezes the language. It is legal **only** at the final output boundary — right before handing text to something that can't accept a `Message`:
- `SpeechManager.Output(...)` when the overload takes `string` (prefer the `Message` overload).
- `buffer.Add(string)`.
- Assigning text to a Godot `Control` (`.Text = ...`, `label.Text = ...`).
- Building an event-template substitution variable that is a `{name}` placeholder in a `Message.Localized("...", new { name = ... })` call (template vars are `string`).
- Writing to a log line (`Log.Info($"... {msg.Resolve()}")`).

Every other `.Resolve()` is wrong and indicates a function that should have returned `Message` instead of `string`. Specifically:
- **Scan for `return foo.Resolve()` or `return Message.Localized(...).Resolve()`** — the enclosing function should return `Message` (or `Message?`).
- **Scan for `var x = msg.Resolve(); ... new Something(x)`** — if `Something` stores the string and hands it to another composer later, `Something` should store `Message` instead.
- **Scan for helpers that return `string` but build their return value through `Message.*` calls** (formatters, summary producers, label helpers). These helpers should return `Message` so callers can continue composing without freezing the language. The Message pipeline's whole point is deferred composition; freezing mid-stack defeats it.
- Refactor chain: change the helper's return type to `Message?`, remove the internal Resolve, update every caller. Callers that were stashing the string (proxy fields, event fields, announcement fields) should also switch to `Message`.

**Missing localization keys:**
- Scan for strings passed to `buffer.Add()`, `SpeechManager.Output()`, or proxy return values that contain English words but don't use localization.
- Check that help message descriptions (TextHelpMessage, ControlHelpMessage) use localized strings.
- Verify new UI strings added by PRs are localized.

**Return type compliance:**
- UIElement `GetLabel`/`GetStatusString`/`GetTooltip`/`GetExtrasString` must return `Message?`, not `string?`.
- Event `GetMessage()` must return `Message?`, not `string?`.
- Container `GetPositionString()` must return `Message?`.
- `Container.ContainerLabel` must be `Message?`, not `string?`.
- Any `*Summary` / `*Label` / `*Description` helper that builds its result from `Message.Localized` calls must return `Message?` (not `string`).

### Code Reuse

**Duplicated logic across files:**
- Search for similar blocks of code in Patches/, UI/Screens/, UI/Elements/, Buffers/.
- Focus on: Harmony patch setup, focus neighbor wiring, buffer population, player name resolution, multiplayer checks.

**Underused utilities:**
- Is `ProxyFactory.Create` handling types it shouldn't? Should some types have dedicated proxies?
- Are there helper methods in one class that should be shared (e.g., `GetPlayerName` exists in multiple files)?
- Is there buffer population logic that's duplicated between proxies and buffers?

### Complexity Hotspots

**Files that are too large or do too much:**
- Identify files over 200 lines. Should they be split?
- Identify methods over 50 lines. Should they be decomposed?
- Identify classes with mixed responsibilities.

**Overly complex methods:**
- Methods with deep nesting (3+ levels).
- Methods with many parameters (5+).
- Methods doing reflection, try/catch, and business logic in the same block.

### Screen & Hook Organization

**Screen coverage:**
- List all game screen types (from decompiled source) and whether the mod has a corresponding GameScreen wrapper.
- Are there screens that should have wrappers but don't?
- Are there screen wrappers that are doing too much (should delegate to separate classes)?

**Help system coverage:**
- List all screens and whether they implement `GetHelpMessages()`.
- Are there screens with non-obvious controls that lack help messages?
- Are all help text/control descriptions localized?

**Hook organization:**
- Are all hooks in the right file? (FocusHooks for focus, ScreenHooks for screens, EventHooks for events, etc.)
- Are there hooks that have grown beyond their original scope?
- Should any hooks file be split?

### Long-term Concerns

**Fragility:**
- What would break if the game updates its UI class hierarchy?
- What would break if the game adds new screen types?
- Are there reflection-based accesses that could be replaced with safer alternatives?

**Scalability:**
- If multiplayer grows (more screens, more events), will the current architecture handle it?
- If more card types or UI elements are added, does ProxyFactory need restructuring?
- Is the settings system scalable to many more event types?

**Compiler warnings:**
- Run `dotnet build` and check for any warnings. The build must produce 0 warnings.
- For CS8602 (null dereference): add null checks or use `?.` operators.
- For CS8604 (null argument): add null guards before the call.
- Do NOT suppress warnings with `#pragma`, `!`, or `[SuppressMessage]`. Fix the underlying null safety issue.

**Technical debt:**
- List any TODO comments or known workarounds in the code.
- Are there temporary fixes that should be made permanent?
- Are there commented-out code blocks that should be removed or restored?

## Output Format

### Summary
- **Critical issues**: [count] (architectural problems that will cause maintenance pain)
- **Improvements**: [count] (refactoring opportunities)
- **Notes**: [count] (minor observations)

### Findings (grouped by severity)
For each finding:
- **Area**: Which audit area it falls under
- **File(s)**: Affected files
- **Description**: What the issue is
- **Impact**: Why it matters for long-term health
- **Suggestion**: Concrete recommendation
