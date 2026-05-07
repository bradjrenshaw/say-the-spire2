# SayTheSpire2 Installer Plan

## Overview
A standalone wxPython GUI app that installs, updates, and uninstalls the SayTheSpire2 mod. Distributed as a single `.exe` via PyInstaller. Fully accessible with screen readers.

## User Flow

### Install / Update
1. Launch installer
2. App auto-detects game install path (or user browses manually)
3. App checks GitHub for latest release version and compares against installed version
4. Main window shows:
   - **No installation found**: Install button labeled "Install"
   - **Installed but outdated**: Install button labeled "Update"
   - **Already up to date**: Install button disabled, status shows "up to date"
5. On Install: download, extract to game directory, enable mods in `settings.save`, show success
6. On Update: open a changelog window showing release notes (`body` field from GitHub API), with "Update" and "Cancel" buttons. If user confirms, download/extract/show success. Cancel returns to main window.

### Uninstall
1. Launch installer, click Uninstall
2. Remove mod files from `<game>/mods/`
3. Optionally remove mod settings from `%APPDATA%/SlayTheSpire2/mods/SayTheSpire2/`
4. Show success message

## Technical Details

### Game Path Detection
1. Try parsing Steam's `libraryfolders.vdf` at `C:/Program Files (x86)/Steam/steamapps/libraryfolders.vdf` — if found, look up the library folder containing the STS2 app ID and construct the game path
2. If vdf not found but `C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/` exists, use that directly
3. If neither works, prompt the user to browse for the game directory
- Validate the selected path by checking for the game executable

### Steam ID / Settings Path
- Settings live at `%APPDATA%/SlayTheSpire2/steam/<steam_id>/settings.save`
- Scan `%APPDATA%/SlayTheSpire2/steam/` for subdirectories — there should be one per user
- If multiple found, use the most recently modified `settings.save`
- The file is JSON; set `mod_settings.mods_enabled` to `true`

### GitHub Release Integration
- Latest release API: `https://api.github.com/repos/bradjrenshaw/say-the-spire2/releases/latest`
- Response includes `tag_name` (version) and `assets[]` with `browser_download_url`
- Find the asset ending in `.zip`
- Direct download URL (no API needed): `https://github.com/bradjrenshaw/say-the-spire2/releases/latest/download/SayTheSpire2.zip`
- Use the API to check version, use the direct URL to download

### Version Tracking
- Store installed version in `%APPDATA%/SlayTheSpire2/mods/SayTheSpire2/version` (plain text file)
- Compare against `tag_name` from GitHub API to determine if update is needed
- Lives alongside existing mod settings — natural location, survives game reinstalls
- Installer creates the directory if it doesn't exist (it's only created by the mod on first settings save)

### Mod Files
The release zip mirrors the game directory structure and is extracted directly to `<game>/`:
```
prism.dll                         # native Prism library (game root)
mods/
  SayTheSpire2.dll                # mod assembly
  SayTheSpire2.pck                # Godot PCK with localization/manifest
  System.Speech.dll               # Windows SAPI dependency
```

## UI Layout (wxPython)

Single window with:
- **Status text** — current state (detected game path, installed version, latest version)
- **Game path field** + Browse button — pre-filled if auto-detected
- **Install/Update button** — downloads and installs latest release
- **Uninstall button** — removes mod files
- **Progress bar** — for download progress
- **Log/output area** — shows what's happening step by step

## Distribution
- Bundle with PyInstaller: `pyinstaller --onefile installer.py`
- Single `SayTheSpire2_Installer.exe`
- No Python installation required for end users
- Host the installer exe on the GitHub releases page alongside the mod zip

## Dependencies
- Python 3.x
- wxPython (GUI)
- requests (HTTP/download)
- vdf (Steam library parsing) — or just parse it manually, it's simple enough
- PyInstaller (build only)

## Edge Cases
- Steam installed to non-default location → manual browse fallback
- Game not yet run (no `settings.save` exists) → create the file with minimal JSON
- No internet connection → show error, allow uninstall only
- GitHub rate limit → use direct download URL as fallback (skips version check)
- Mod files partially present → overwrite all files on install
