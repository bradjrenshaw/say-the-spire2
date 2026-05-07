"""
Localization workflow helper for SayTheSpire2.

Locale files are sparse: a key only appears once it has a real translation.
Anything missing falls back to the eng table at runtime
(see Localization/LocalizationManager.cs:42-61). Keeping locales additive
means an eng-side rewording never leaves a stale English copy stranded in
some other language — the missing key just silently re-resolves to the new
eng value.

Subcommands:

  list                       List locale folders + per-file key counts.
  init <code>                Scaffold Localization/<code>/{ui,map_nav}.json
                             as empty `{}` files. Locale starts fully
                             English-via-fallback; translations are added
                             additively.
  seed <code>                Optional, opt-in. Copy eng values into locale
                             for keys not yet present. Never overwrites
                             existing translations. Useful for translators
                             who want to edit values in place; comes with
                             the cost that eng-side rewordings won't be
                             reflected automatically.
  audit [<code>]             Per-locale breakdown:
                               translated   (in locale, differs from eng)
                               eng-equiv    (in locale, value == eng)
                               missing      (absent from locale)
                               stale        (in locale, not in eng)
                               placeholder  ({foo} mismatches vs eng)
                             With no arg, audits every non-eng locale.
  extract <code>             Print a JSON blob of just the missing keys,
    [--file ui.json]         paired with eng source values, for handoff to
                             a translator (or LLM). Defaults to all files.
  apply <code> <path>        Merge a flat key->value JSON back into the
    [--file ui.json]         locale. Refuses to overwrite existing
    [--force]                translations unless --force. Validates that
                             {placeholder} sets match the eng source.

Usage:
    python scripts/locale_tool.py <command> [args]

Examples:
    python scripts/locale_tool.py list
    python scripts/locale_tool.py init fra
    python scripts/locale_tool.py audit rus
    python scripts/locale_tool.py extract fra > /tmp/fra-todo.json
    python scripts/locale_tool.py apply fra /tmp/fra-translated.json
"""
import argparse
import json
import re
import sys
from pathlib import Path


PLACEHOLDER_RE = re.compile(r"\{(\w+)\}")
LOCALE_FILES = ("ui.json", "map_nav.json")


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def localization_dir() -> Path:
    return repo_root() / "Localization"


def eng_dir() -> Path:
    return localization_dir() / "eng"


def locale_dir(code: str) -> Path:
    return localization_dir() / code


def placeholders(value: str) -> set[str]:
    return set(PLACEHOLDER_RE.findall(value))


def load_json(path: Path) -> dict:
    if not path.exists():
        return {}
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def all_locale_codes() -> list[str]:
    return sorted(p.name for p in localization_dir().iterdir() if p.is_dir())


# -- commands ---------------------------------------------------------------


def cmd_list(_args: argparse.Namespace) -> int:
    eng_files = {f: load_json(eng_dir() / f) for f in LOCALE_FILES}
    eng_counts = {f: len(eng_files[f]) for f in LOCALE_FILES}

    print(f"{'locale':<8}", *(f"{f:>14}" for f in LOCALE_FILES), sep="  ")
    print(f"{'eng':<8}", *(f"{eng_counts[f]:>14}" for f in LOCALE_FILES), sep="  ")
    for code in all_locale_codes():
        if code == "eng":
            continue
        counts = []
        for f in LOCALE_FILES:
            data = load_json(locale_dir(code) / f)
            counts.append(f"{len(data):>14}")
        print(f"{code:<8}", *counts, sep="  ")
    return 0


def cmd_init(args: argparse.Namespace) -> int:
    code = args.code
    if code == "eng":
        print("error: cannot init eng — it is the source of truth.", file=sys.stderr)
        return 2

    target = locale_dir(code)
    target.mkdir(parents=True, exist_ok=True)
    for fname in LOCALE_FILES:
        path = target / fname
        if path.exists() and load_json(path):
            print(f"  skip {fname}: already populated")
            continue
        write_json(path, {})
        print(f"  wrote empty {path.relative_to(repo_root())}")
    print(f"\nLocale '{code}' is fully English via fallback. Translate by")
    print(f"adding keys to {target.relative_to(repo_root())}/<file>.json.")
    return 0


def cmd_seed(args: argparse.Namespace) -> int:
    code = args.code
    if code == "eng":
        print("error: cannot seed eng.", file=sys.stderr)
        return 2

    target = locale_dir(code)
    if not target.exists():
        print(f"error: {target} does not exist. Run init first.", file=sys.stderr)
        return 2

    total_added = 0
    for fname in LOCALE_FILES:
        eng_path = eng_dir() / fname
        loc_path = target / fname
        if not eng_path.exists():
            continue
        eng = load_json(eng_path)
        loc = load_json(loc_path)
        added = 0
        for key, val in eng.items():
            if key not in loc:
                loc[key] = val
                added += 1
        if added:
            write_json(loc_path, loc)
            print(f"  {fname}: seeded {added} key(s) from eng")
        else:
            print(f"  {fname}: nothing to seed")
        total_added += added
    print(f"\nSeeded {total_added} key(s) total. Translator should edit in place.")
    print("Note: eng-side rewordings won't be reflected automatically. If a")
    print("seeded value diverges from eng later, audit will flag it as")
    print("eng-equivalent (matches old eng) — review when re-syncing.")
    return 0


def cmd_audit(args: argparse.Namespace) -> int:
    codes = [args.code] if args.code else [c for c in all_locale_codes() if c != "eng"]
    for code in codes:
        if code == "eng":
            continue
        if not locale_dir(code).exists():
            print(f"\n=== {code}: not found", file=sys.stderr)
            continue

        print(f"\n=== {code} ===")
        for fname in LOCALE_FILES:
            eng = load_json(eng_dir() / fname)
            loc = load_json(locale_dir(code) / fname)
            translated, eng_equiv, missing, stale, ph_mismatch = [], [], [], [], []
            for key, eng_val in eng.items():
                if key not in loc:
                    missing.append(key)
                elif loc[key] == eng_val:
                    eng_equiv.append(key)
                else:
                    translated.append(key)
                    if placeholders(loc[key]) != placeholders(eng_val):
                        ph_mismatch.append(key)
            for key in loc:
                if key not in eng:
                    stale.append(key)

            print(f"  {fname}:")
            print(f"    translated:  {len(translated)}")
            print(f"    eng-equiv:   {len(eng_equiv)}")
            print(f"    missing:     {len(missing)}")
            print(f"    stale:       {len(stale)}")
            if ph_mismatch:
                print(f"    placeholder mismatches: {len(ph_mismatch)}")
                for k in ph_mismatch:
                    print(f"      - {k}: eng={sorted(placeholders(eng[k]))} loc={sorted(placeholders(loc[k]))}")
            if args.verbose and missing:
                print(f"    missing keys:")
                for k in missing:
                    print(f"      - {k}")
            if args.verbose and stale:
                print(f"    stale keys:")
                for k in stale:
                    print(f"      - {k}")
    return 0


def cmd_extract(args: argparse.Namespace) -> int:
    code = args.code
    if code == "eng":
        print("error: cannot extract eng.", file=sys.stderr)
        return 2
    if not locale_dir(code).exists():
        print(f"error: {locale_dir(code)} does not exist.", file=sys.stderr)
        return 2

    files = [args.file] if args.file else list(LOCALE_FILES)
    output: dict[str, dict[str, str]] = {}
    for fname in files:
        eng = load_json(eng_dir() / fname)
        loc = load_json(locale_dir(code) / fname)
        missing = {k: v for k, v in eng.items() if k not in loc}
        if missing:
            output[fname] = missing

    json.dump(output, sys.stdout, ensure_ascii=False, indent=2)
    sys.stdout.write("\n")
    return 0


def cmd_apply(args: argparse.Namespace) -> int:
    code = args.code
    if code == "eng":
        print("error: cannot apply to eng.", file=sys.stderr)
        return 2

    target = locale_dir(code)
    if not target.exists():
        print(f"error: {target} does not exist. Run init first.", file=sys.stderr)
        return 2

    src_path = Path(args.path)
    if not src_path.exists():
        print(f"error: {src_path} not found.", file=sys.stderr)
        return 2

    src = load_json(src_path)

    # Two accepted shapes:
    #   {"ui.json": {key: val, ...}, "map_nav.json": {...}}     # extract output
    #   {key: val, ...}                                          # flat — needs --file
    if all(isinstance(v, dict) for v in src.values()):
        per_file = src
    elif args.file:
        per_file = {args.file: src}
    else:
        print("error: flat translations.json requires --file <name>.", file=sys.stderr)
        return 2

    summary = []
    for fname, translations in per_file.items():
        eng = load_json(eng_dir() / fname)
        loc_path = target / fname
        loc = load_json(loc_path)

        added = updated = skipped = ph_mismatch = unknown = 0
        for key, val in translations.items():
            if key not in eng:
                unknown += 1
                continue
            if placeholders(val) != placeholders(eng[key]):
                ph_mismatch += 1
                print(f"  WARN {fname}/{key}: placeholders {sorted(placeholders(val))} != eng {sorted(placeholders(eng[key]))}")
            if key in loc and not args.force:
                if loc[key] == val:
                    skipped += 1
                else:
                    skipped += 1
                    print(f"  SKIP {fname}/{key}: already translated (use --force to overwrite)")
                continue
            if key in loc:
                updated += 1
            else:
                added += 1
            loc[key] = val

        # Preserve insertion order matching eng for keys we know about,
        # then keep any locale-only keys at the end (rare; flagged by audit).
        ordered: dict[str, str] = {}
        for k in eng:
            if k in loc:
                ordered[k] = loc[k]
        for k in loc:
            if k not in ordered:
                ordered[k] = loc[k]

        write_json(loc_path, ordered)
        summary.append((fname, added, updated, skipped, ph_mismatch, unknown))

    print()
    for fname, added, updated, skipped, ph_mismatch, unknown in summary:
        print(f"  {fname}: +{added} added, ~{updated} updated, {skipped} skipped, "
              f"{ph_mismatch} placeholder warnings, {unknown} unknown keys")
    return 0


# -- entry point ------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser(
        prog="locale_tool",
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    sub = parser.add_subparsers(dest="cmd", required=True)

    sub.add_parser("list").set_defaults(func=cmd_list)

    p_init = sub.add_parser("init", help="Scaffold an empty locale folder.")
    p_init.add_argument("code")
    p_init.set_defaults(func=cmd_init)

    p_seed = sub.add_parser("seed", help="Fill missing keys with eng values (opt-in).")
    p_seed.add_argument("code")
    p_seed.set_defaults(func=cmd_seed)

    p_audit = sub.add_parser("audit", help="Report locale coverage vs eng.")
    p_audit.add_argument("code", nargs="?")
    p_audit.add_argument("-v", "--verbose", action="store_true")
    p_audit.set_defaults(func=cmd_audit)

    p_extract = sub.add_parser("extract", help="Dump missing keys as JSON for translation.")
    p_extract.add_argument("code")
    p_extract.add_argument("--file", choices=LOCALE_FILES)
    p_extract.set_defaults(func=cmd_extract)

    p_apply = sub.add_parser("apply", help="Merge translations back into a locale.")
    p_apply.add_argument("code")
    p_apply.add_argument("path")
    p_apply.add_argument("--file", choices=LOCALE_FILES)
    p_apply.add_argument("--force", action="store_true",
                         help="Overwrite existing translations.")
    p_apply.set_defaults(func=cmd_apply)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
