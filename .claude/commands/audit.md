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
