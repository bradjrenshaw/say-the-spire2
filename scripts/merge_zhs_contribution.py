"""
Merge a Chinese-translation contribution into our current zhs/ files.

Strategy: walk our current eng/{file}.json keys in order; for each key prefer
the contributor's translation when present, fall back to whatever's already
in our current zhs/{file}.json, and as last resort the English value (so the
file is never missing a key). Keys the contributor translated but that no
longer exist in our eng file are reported as stale and dropped. Template
variables ({foo}) are diffed so we catch keys whose substitutions shifted
between when the contributor translated and now.

Usage:
    python scripts/merge_zhs_contribution.py path/to/contribution_dir
"""
import json
import re
import sys
from pathlib import Path


PLACEHOLDER_RE = re.compile(r"\{(\w+)\}")


def placeholders(value: str) -> set[str]:
    return set(PLACEHOLDER_RE.findall(value))


def merge_file(eng: dict, current_zhs: dict, contrib: dict, label: str) -> dict:
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
        elif key in current_zhs:
            merged[key] = current_zhs[key]
            kept_existing += 1
        else:
            merged[key] = eng_val
            fell_back_to_eng += 1

    for key in contrib:
        if key not in eng:
            stale.append(key)

    print(f"\n=== {label} ===")
    print(f"  used contributor translation:   {used}")
    print(f"  kept current zhs translation:   {kept_existing}")
    print(f"  fell back to English:           {fell_back_to_eng}")
    print(f"  contributor keys no longer in eng (dropped): {len(stale)}")
    if stale:
        for k in stale:
            print(f"    - {k}")
    if placeholder_mismatch:
        print(f"  WARNING: placeholder mismatch in {len(placeholder_mismatch)} key(s) — contributor translation kept, please spot-check:")
        for k in placeholder_mismatch:
            print(f"    - {k}: eng={sorted(placeholders(eng[k]))} contrib={sorted(placeholders(contrib[k]))}")
    return merged


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2

    contrib_dir = Path(sys.argv[1])
    repo_root = Path(__file__).resolve().parent.parent
    eng_dir = repo_root / "Localization" / "eng"
    zhs_dir = repo_root / "Localization" / "zhs"

    for filename in ("ui.json", "map_nav.json"):
        eng_path = eng_dir / filename
        zhs_path = zhs_dir / filename
        contrib_path = contrib_dir / filename

        if not eng_path.exists():
            print(f"skip {filename}: no eng file")
            continue
        if not contrib_path.exists():
            print(f"skip {filename}: no contributor file at {contrib_path}")
            continue

        with eng_path.open("r", encoding="utf-8") as f:
            eng = json.load(f)
        with zhs_path.open("r", encoding="utf-8") as f:
            current_zhs = json.load(f)
        with contrib_path.open("r", encoding="utf-8") as f:
            contrib = json.load(f)

        merged = merge_file(eng, current_zhs, contrib, filename)

        with zhs_path.open("w", encoding="utf-8", newline="\n") as f:
            json.dump(merged, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"  wrote {zhs_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
