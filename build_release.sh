#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Building mod (Release) ==="
dotnet build -c Release

echo "=== Building documentation ==="
mdbook build docs_src

echo "=== Adding docs to release zip ==="
uv run python scripts/add_docs_to_release.py

# --- Loadstone: branded standalone installer + release manifest -------------
# Loadstone lives in the loadstone/ git submodule (override with
# MODMANAGER_DIR to use another checkout). Building the installer exe needs
# the wx/CMake toolchain from a VS developer environment; WX_DIR points at
# the static wxWidgets install.
MODMANAGER_DIR="${MODMANAGER_DIR:-$SCRIPT_DIR/loadstone}"
WX_DIR="${WX_DIR:-C:/Users/bradj/code/wx-static}"
VERSION=$(python -c "import json;print(json.load(open('SayTheSpire2.json'))['version'])")

if [ ! -f "$MODMANAGER_DIR/Cargo.toml" ]; then
    echo "=== Fetching loadstone submodule ==="
    git -C "$SCRIPT_DIR" submodule update --init loadstone
fi

# Everything the GitHub release needs lands in release/: the notes file at
# its top level and every uploadable asset in release/assets/, so publishing
# is one command (see the summary below).
RELEASE_DIR="$SCRIPT_DIR/release"
ASSETS_DIR="$RELEASE_DIR/assets"
rm -rf "$RELEASE_DIR"
mkdir -p "$ASSETS_DIR"
cp changes-latest.md "$RELEASE_DIR/"
cp SayTheSpire2.zip "$ASSETS_DIR/"

echo "=== Building Loadstone standalone installer (v$VERSION) ==="
cmake -S "$MODMANAGER_DIR/app" -B "$MODMANAGER_DIR/build" -G Ninja     -DCMAKE_BUILD_TYPE=Release     -DCMAKE_PREFIX_PATH="$WX_DIR"     -DLOADSTONE_EMBED_PROFILE="$SCRIPT_DIR/installer/loadstone-profile.json" >/dev/null
cmake --build "$MODMANAGER_DIR/build" >/dev/null
cp "$MODMANAGER_DIR/build/loadstone-manager.exe" "$ASSETS_DIR/SayTheSpire2Installer.exe"

# Git Bash's GNU /usr/bin/link shadows MSVC's link.exe, which breaks cargo
# build scripts; put the MSVC bin directory (where cl.exe lives) first.
if command -v cl.exe >/dev/null 2>&1; then
    PATH="$(dirname "$(command -v cl.exe)"):$PATH"
fi

echo "=== Building loadstone-release.json ==="
(cd "$MODMANAGER_DIR" && cargo build --release -p loadstone-cli >/dev/null)
"$MODMANAGER_DIR/target/release/loadstone.exe" package --version "v$VERSION"     --artifact "$ASSETS_DIR/SayTheSpire2.zip"     --installer "$ASSETS_DIR/SayTheSpire2Installer.exe"     --notes-file "$RELEASE_DIR/changes-latest.md"     --out "$ASSETS_DIR/loadstone-release.json"

echo "=== Done ==="
echo "Release folder: release/ (notes: changes-latest.md, assets: assets/)"
echo "Publish with:   gh release create v$VERSION --title V$VERSION --notes-file release/changes-latest.md release/assets/*"
