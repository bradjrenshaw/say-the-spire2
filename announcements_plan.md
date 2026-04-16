# Focus Announcements Refactor — Plan

## Motivation

The focus-string system in `UIElement` has grown organically. Today each element overrides `GetLabel / GetExtrasString / GetTypeKey / GetSubtypeKey / GetStatusString / GetTooltip`, with additional `CollectPreExtras / CollectPostExtras` events for bolt-on fields. The composer assembles them into a fixed template:

```
{label} {extras1}, {subtype} {type} {status}, {extras2}, {tooltip}
```

Three problems:

1. **Convoluted layout.** New fields get shoved into whichever slot approximately fits (often `GetStatusString`, which string-concatenates HP, block, and intent inside one method — with per-element custom logic like `intent_first`). Hard to reason about, hard to extend.
2. **Low configurability.** Users can toggle type/subtype/tooltip announcement per element type, and that's it. No per-field granularity, no reordering, no per-field verbose toggles.
3. **Duplicated game-state lookup.** Each proxy re-implements how to reach its underlying model: walking `Control.GetParent()` chains, reflecting private game fields, querying `RunManager.HoveredModelTracker`. Events do the same lookups again. This has been the source of multiple bugs and is the reason `ProxyCreature.GetIntentSummary` is `public static` — so other code can reuse it.

We're refactoring both the data-access story and the presentation story.

## Target architecture — four layers

```
Proxy (director)
  │
  │  yields
  ▼
Announcement (presentation unit)
  │
  │  reads
  ▼
View (data wrapper over game state)
  │
  │  wraps
  ▼
Game objects (Creature, CardModel, RelicModel, ...)
```

Plus a **Composer** that takes `IEnumerable<Announcement>`, filters by settings, orders by declaration, joins with per-announcement suffixes and spaces.

### View layer — data

One class per game-domain concept: `CreatureView`, `CardView`, `RelicView`, `PotionView`, `PowerView`, etc.

- Owns all reflection and parent-walking logic.
- Constructible from whatever the caller has (e.g., `CreatureView.FromControl(control)`, `CreatureView.FromEntity(creature)`).
- Exposes typed structured data (`Name`, `Hp.Current`, `Hp.Max`, `Block`, `Intents`, `Powers`, `IsLocalPlayer`, ...).
- No speech, no localization, no settings awareness.
- Reusable by Announcements, Buffers, Events, and tests.

Named `View` because `State` collides with existing game-side naming conventions.

### Announcement layer — presentation

One class per semantic concept: `LabelAnnouncement`, `TypeAnnouncement`, `HpAnnouncement`, `BlockAnnouncement`, `IntentsAnnouncement`, `PowersAnnouncement`, `TooltipAnnouncement`, `EnergyCostAnnouncement`, `StarCostAnnouncement`, etc.

- Takes **primitive values** in its constructor — not a View reference. Proxy extracts data and passes in what the announcement needs.
- Has a stable string `Key` ("hp", "intents", ...) used for settings paths and introspection.
- Renders to a `Message`.
- Declares its own settings (via static `RegisterSettings`, same pattern as events).
- `HpAnnouncement` is **the** hp rendering logic — used by every proxy that shows HP, every buffer, every event. Change once, affect everywhere.

Sketch:

```csharp
public abstract class Announcement
{
    public abstract string Key { get; }
    public abstract Message Render();
}

public class HpAnnouncement : Announcement
{
    private readonly int _current;
    private readonly int _max;
    public HpAnnouncement(int current, int max) { _current = current; _max = max; }

    public override string Key => "hp";
    public override Message Render() =>
        Message.Localized("ui", "RESOURCE.HP", new { current = _current, max = _max });

    public static void RegisterSettings(CategorySetting cat)
    {
        cat.Add(new BoolSetting("enabled", "Announce", true));
        cat.Add(new StringSetting("suffix", "Suffix", ","));
    }
}
```

### Proxy layer — director

The proxy class (e.g., `ProxyCreature`) becomes a thin director:

```csharp
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(HpAnnouncement),
    typeof(BlockAnnouncement),
    typeof(IntentsAnnouncement),
    typeof(PowersAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyCreature : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = CreatureView.FromControl(Control);
        if (view == null) yield break;

        yield return new LabelAnnouncement(view.Name);
        yield return new TypeAnnouncement("creature");
        yield return new HpAnnouncement(view.Hp.Current, view.Hp.Max);
        if (view.Block > 0)
            yield return new BlockAnnouncement(view.Block);
        yield return new IntentsAnnouncement(view.Intents);
        yield return new PowersAnnouncement(view.Powers);
        if (view.Description != null)
            yield return new TooltipAnnouncement(view.Description);
    }
}
```

No formatting logic. No reflection. No direct game-state access. Easy to read, easy to diff.

### Composer — orchestrator

Replaces the current `BuildLabelPart / BuildTypePart / GetFocusMessage` logic. Responsibilities:

1. Read the `[AnnouncementOrder]` declaration for this element type — defines the preferred ordering.
2. Collect `GetFocusAnnouncements()` output.
3. Sort yielded announcements by the declared order (or user-override order if set). Anything yielded that isn't in the declaration goes at the end in yield order — proxies can add/remove announcements without having to touch the attribute to avoid a crash, and the attribute stays a hint rather than a contract.
4. For each announcement: check effective `enabled` (per-element override → global default). Skip if disabled.
5. Render each and append its effective `suffix`.
6. Space-join the results.

Separator model: everything space-joined. Each announcement's suffix is appended to its rendered text before joining. Example: `HpAnnouncement.Render()` returns `"50/80 HP"`, suffix is `","`, so the piece emitted is `"50/80 HP,"`. Then space-join with the next piece.

## Settings — cascade model

Two levels with `null` sentinel inheritance.

**Global defaults** (one per announcement type):
```
announcements.hp.enabled = true            (bool)
announcements.hp.suffix  = ","             (string)
announcements.intents.enabled = true
announcements.intents.suffix  = ","
announcements.intents.verbose = false
...
```

**Per-element overrides** (one per announcement-on-element combo):
```
ui.creature.hp.enabled = null              (bool?   null = inherit)
ui.creature.hp.suffix  = null              (string? null = inherit)
ui.creature.intents.verbose = null
...
```

**Resolution:** per-element if not null; otherwise global.

**UI:**
- Checkbox/text input shows the *resolved* value. User never sees "inherit" as a third state.
- Toggling always writes an explicit value at the per-element level.
- Changing the global propagates only to per-element entries still at `null`; explicit overrides stay put.
- Each `UI/{element}/` settings page has a **"Reset announcements to defaults"** button that clears every per-element override under it back to `null`. This is the only way to get back to inheriting (no implicit collapse).

**String inherit subtlety:** `StringSetting` must distinguish `null` (inherit) from `""` (explicit empty). A user who wants *no* punctuation sets the text field to empty; a user who hasn't touched it has `null`. Godot `LineEdit.Text` defaults to `""`, so we need an explicit "has value" flag alongside the text.

**Localization (hard requirement).** Every setting introduced by this refactor — category labels, setting labels, choice option labels, help text, the reset button text — must be localized. No hardcoded English passed into `BoolSetting` / `StringSetting` / `CategorySetting` constructors.

Implementation: setting constructors take an **explicit full localization key** for their label. No derivation, no convention, no special-casing. Every registration site passes the key. Labels are resolved from `Localization/eng/ui.json` at render time. Missing-key fallback logs a warning and renders the key itself as the visible label so typos are immediately obvious.

Shared labels reuse existing keys naturally — if two settings both render as "Enabled" or "Verbose", they pass the same existing key. No separate framework for "universal vs specific" settings; it's just keys all the way down.

**Out of scope:** existing settings that hardcode English today (e.g., event `"Announce"` / `"Add to buffer"` labels). Retroactively localizing them is a separate follow-up. This refactor ensures the *new* settings are localized and provides the mechanism that a follow-up could adopt.

## Phasing

Three phases. Each is shippable on its own; each is reversible without blocking the next.

### Phase 1 — View layer

**Goal:** close the duplicated-lookup bug class without changing any public API.

- Create `Views/CreatureView.cs`, `Views/CardView.cs`, `Views/RelicView.cs`, `Views/PotionView.cs`, `Views/PowerView.cs`, etc.
- Each owns the reflection targets currently scattered across proxies (CLAUDE.md's "Critical Reflection Targets" section is the starting inventory).
- Migrate proxies to use views internally. Keep existing `GetLabel / GetExtrasString / ...` signatures. No behavior change visible to the user.
- Migrate events that currently duplicate lookups (e.g., `PowerEvent`, `HpEvent`, and anything that calls `ProxyCreature.GetIntentSummary`) to use views.
- Delete `public static` helpers that existed only for cross-proxy reuse (e.g., `ProxyCreature.GetIntentSummary`) — the view is the sharing point now.

**Deliverable:** zero user-visible change, but every game-state lookup goes through one code path. Unit-testable.

**Risk:** low. Pure internal refactor. Covered by the existing focus-string tests (speech output should be identical before/after).

### Phase 2 — Announcement layer

**Goal:** replace the fixed focus-string template with the Announcement pipeline.

- Add `Announcement` base class, `AnnouncementOrderAttribute`, `AnnouncementComposer`.
- Create one Announcement subclass per semantic concept. Initial set based on what today's proxies emit — label, type, subtype (if we keep it — see open question), status fields (hp, block, intent, powers, ...), tooltip, costs (energy, stars), position info where applicable.
- Add `UIElement.GetFocusAnnouncements()` with a default implementation that wraps the existing `GetLabel / GetExtrasString / ...` output into `LegacyAnnouncement` shims. This keeps unmigrated proxies working unchanged.
- Add `[AnnouncementOrder]` to proxies as they migrate. Override `GetFocusAnnouncements()` per proxy; delete the old `GetLabel` / `GetExtrasString` / etc. overrides from that proxy.
- Replace `UIElement.GetFocusMessage()` to call the composer.
- Migrate proxies in small batches. Verify speech output at each step against a test harness.

**Deliverable:** focus strings emitted by the new pipeline; every migrated proxy has a clean director-only shape. Behavior unchanged at the user level (settings not yet granular).

**Risk:** medium. Touches every proxy eventually. Mitigated by the legacy-shim default so migration is incremental.

### Phase 3 — Per-announcement settings + cascade

**Goal:** unlock the user-visible configurability win.

- Add `StringSetting` (if not already present) with null-vs-empty distinction. Add text-input UI via `ProxyTextInput`/`LineEdit`.
- Announcement registration pipeline:
  1. Scan assembly for `Announcement` subclasses; for each, create `announcements.{key}/` category and call the announcement's `RegisterSettings`.
  2. For each proxy type with `[AnnouncementOrder]`: for each announcement in its order list, create `ui.{element}.{key}/` with an override version of every setting the announcement declared (all default `null`, i.e. inherit).
- Resolution helper: `AnnouncementSettings.Resolve(elementKey, announcementKey, settingKey)` reads per-element first, falls back to global.
- Composer reads enabled/suffix/verbose via the resolver.
- Add "Reset announcements to defaults" button to each `UI/{element}/` settings screen.
- Post-phase stretch: settings-based reordering per element type (stored as ordered list of keys; falls back to `[AnnouncementOrder]` default).

**Deliverable:** users can toggle any announcement globally or per-element, customize suffix, toggle verbose, and reset overrides.

**Risk:** low-to-medium. Mostly settings plumbing; UI work for the text input and reset button.

## Migration and backward compat

During phase 2, old (legacy) proxies and new (announcement) proxies coexist. The `LegacyAnnouncement` default implementation of `GetFocusAnnouncements()` wraps `GetLabel`/`GetExtrasString`/etc. output so unmigrated proxies keep working. Migration is strictly additive per proxy — flip one at a time, test speech, commit.

The `CollectPreExtras` / `CollectPostExtras` event-based extension points disappear in the new model (proxies yield what they want; no bolt-on event pattern needed). Anything using them today needs migration during phase 2.

## Open questions (to resolve during implementation, not now)

1. **Subtype.** Current system has `GetSubtypeKey()` for card type ("attack", "skill") and announces it with special ordering relative to type. In the new model, is this a separate `SubtypeAnnouncement`, a property on `TypeAnnouncement`, or absorbed into the proxy's choice of which `TypeAnnouncement` to emit? Defer; decide during card-proxy migration.
2. **Container path diffing and position announcements.** `FocusContext` builds "Name, X of Y" prefixes from container paths. Unrelated to focus-string content per se, but will interact with the composer. Probably stays as-is, invoked around the composer's output. Flag during phase 2.
3. **Buffers.** `HandleBuffers()` is orthogonal but may also consume views (e.g., `CreatureBuffer` would read a `CreatureView`). Not in scope for this refactor; separate follow-up.
4. **User-configurable reordering.** Phase 3 stretch. May defer to a phase 4 if phase 3 is already large.
5. **Per-announcement settings beyond enabled/suffix.** Some announcements have semantic toggles (`IntentsAnnouncement.verbose`, `EnergyCostAnnouncement.verbose_costs`). Each announcement owns its own `RegisterSettings`; no framework special-casing needed.

## Success criteria

- Phase 1: zero-diff speech output; all game-state lookups go through `Views/`; old `public static` proxy helpers gone.
- Phase 2: every proxy is a thin director; focus-string composition lives in one place (`AnnouncementComposer`); old fixed-template logic removed.
- Phase 3: user can toggle announcement X globally or on element type Y independently; users can reset per-element overrides; no implicit collapse of user-set values across updates; every new setting label, description, and choice option is localized via `Localization/eng/ui.json` with no hardcoded English in settings constructors.
