"""
Merge a translation contribution into a Localization/<locale>/ folder.

Strategy: walk our current eng/{file}.json keys in order; for each key prefer
the contributor's translation when present, fall back to whatever's already
in our current <locale>/{file}.json, and as last resort the English value
(so the file is never missing a key). Keys the contributor translated but
that no longer exist in our eng file are reported as stale and dropped.
Template variables ({foo}) are diffed so we catch keys whose substitutions
shifted between when the contributor translated and now.

Usage:
    python scripts/merge_locale_contribution.py <locale> <path/to/contribution_dir>

Examples:
    python scripts/merge_locale_contribution.py zhs ../zhs_contribution
    python scripts/merge_locale_contribution.py rus ../rus_contribution
"""
import json
import re
import sys
from pathlib import Path


PLACEHOLDER_RE = re.compile(r"\{(\w+)\}")


def placeholders(value: str) -> set[str]:
    return set(PLACEHOLDER_RE.findall(value))


def merge_file(eng: dict, current_locale: dict, contrib: dict, label: str, locale: str) -> dict:
    used = 0
    kept_existing = 0
    fell_back_to_eng = 0
    placeholder_mismatch: list[str] = []
    stale: list[str] = []

    merged: dict[str, str] = {}
    for key, eng_val in eng.items():
        if key in contrib:
            contrib_val = contrib[key]
            if placeholders(contrib_val) != placeholders(eng_val):
                placeholder_mismatch.append(key)
            merged[key] = contrib_val
            used += 1
        elif key in current_locale:
            merged[key] = current_locale[key]
            kept_existing += 1
        else:
            merged[key] = eng_val
            fell_back_to_eng += 1

    for key in contrib:
        if key not in eng:
            stale.append(key)

    print(f"\n=== {label} ===")
    print(f"  used contributor translation:    {used}")
    print(f"  kept current {locale} translation:    {kept_existing}")
    print(f"  fell back to English:            {fell_back_to_eng}")
    print(f"  contributor keys no longer in eng (dropped): {len(stale)}")
    if stale:
        for k in stale:
            print(f"    - {k}")
    if placeholder_mismatch:
        print(f"  WARNING: placeholder mismatch in {len(placeholder_mismatch)} key(s) - contributor translation kept, please spot-check:")
        for k in placeholder_mismatch:
            print(f"    - {k}: eng={sorted(placeholders(eng[k]))} contrib={sorted(placeholders(contrib[k]))}")
    return merged


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__)
        return 2

    locale = sys.argv[1]
    contrib_dir = Path(sys.argv[2])
    repo_root = Path(__file__).resolve().parent.parent
    eng_dir = repo_root / "Localization" / "eng"
    locale_dir = repo_root / "Localization" / locale
    locale_dir.mkdir(parents=True, exist_ok=True)

    for filename in ("ui.json", "map_nav.json"):
        eng_path = eng_dir / filename
        locale_path = locale_dir / filename
        contrib_path = contrib_dir / filename

        if not eng_path.exists():
            print(f"skip {filename}: no eng file")
            continue
        if not contrib_path.exists():
            print(f"skip {filename}: no contributor file at {contrib_path}")
            continue

        with eng_path.open("r", encoding="utf-8") as f:
            eng = json.load(f)
        if locale_path.exists():
            with locale_path.open("r", encoding="utf-8") as f:
                current_locale = json.load(f)
        else:
            current_locale = {}
        with contrib_path.open("r", encoding="utf-8") as f:
            contrib = json.load(f)

        merged = merge_file(eng, current_locale, contrib, filename, locale)

        with locale_path.open("w", encoding="utf-8", newline="\n") as f:
            json.dump(merged, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"  wrote {locale_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
