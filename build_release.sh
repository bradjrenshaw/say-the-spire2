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

echo "=== Building Loadstone standalone installer (v$VERSION) ==="
cmake -S "$MODMANAGER_DIR/app" -B "$MODMANAGER_DIR/build" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_PREFIX_PATH="$WX_DIR" \
    -DLOADSTONE_EMBED_PROFILE="$SCRIPT_DIR/installer/loadstone-profile.json" >/dev/null
cmake --build "$MODMANAGER_DIR/build" >/dev/null
cp "$MODMANAGER_DIR/build/loadstone-manager.exe" "$SCRIPT_DIR/SayTheSpire2Installer.exe"

echo "=== Building loadstone-release.json ==="
(cd "$MODMANAGER_DIR" && cargo build --release -p loadstone-cli >/dev/null)
"$MODMANAGER_DIR/target/release/loadstone.exe" package --version "v$VERSION" \
    --artifact SayTheSpire2.zip \
    --installer SayTheSpire2Installer.exe \
    --notes-file changes-latest.md

echo "=== Done ==="
echo "Release zip:      SayTheSpire2.zip"
echo "Installer:        SayTheSpire2Installer.exe"
echo "Release manifest: loadstone-release.json (attach both + this to the GitHub release)"
