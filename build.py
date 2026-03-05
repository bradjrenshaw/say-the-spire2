"""
Build script for the STS2 Accessibility Mod.
1. Runs dotnet build
2. Creates a minimal PCK file containing mod_manifest.json
3. Copies the DLL and PCK to the game's mods/ directory
"""
import struct
import json
import os
import subprocess
import sys
import shutil

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MOD_NAME = "sts2-accessibility-mod"
GAME_DIR = r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
MODS_DIR = os.path.join(GAME_DIR, "mods")
BUILD_DIR = os.path.join(SCRIPT_DIR, "bin", "Release", "net9.0", "win-x64")


def build_dll():
    print("Building DLL...")
    result = subprocess.run(
        ["dotnet", "build", "-c", "Release"],
        cwd=SCRIPT_DIR,
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.returncode != 0:
        print("BUILD FAILED:")
        print(result.stderr)
        sys.exit(1)
    print("Build succeeded.")


def create_pck(output_path: str):
    """Create a minimal Godot PCK file containing just the mod manifest."""
    manifest_path = os.path.join(SCRIPT_DIR, "mod_manifest.json")
    with open(manifest_path, "rb") as f:
        manifest_data = f.read()

    # PCK file path inside the archive (must be res://mod_manifest.json)
    res_path = "res://mod_manifest.json"
    # Pad path to 4-byte alignment
    path_bytes = res_path.encode("utf-8")
    path_padded_len = (len(path_bytes) + 4) & ~3  # align to 4 bytes
    path_bytes_padded = path_bytes.ljust(path_padded_len, b"\x00")

    # Calculate offsets
    # Header: 4 (magic) + 4 (ver) + 4*3 (engine) + 4 (flags) + 8 (file_base) + 64 (reserved) = 96
    # File count: 4
    # File entry: 4 (path_len) + path_padded_len + 8 (offset) + 8 (size) + 16 (md5) + 4 (flags)
    header_size = 96
    file_count_size = 4
    file_entry_size = 4 + path_padded_len + 8 + 8 + 16 + 4
    directory_size = file_count_size + file_entry_size
    data_offset = header_size + directory_size

    # Align data_offset to 64 bytes (Godot expects alignment)
    data_offset_aligned = (data_offset + 63) & ~63

    with open(output_path, "wb") as f:
        # Header
        f.write(b"GDPC")  # magic
        f.write(struct.pack("<I", 2))  # pack format version
        f.write(struct.pack("<I", 4))  # engine major
        f.write(struct.pack("<I", 5))  # engine minor
        f.write(struct.pack("<I", 1))  # engine patch
        f.write(struct.pack("<I", 0))  # flags (0 = no encryption, absolute paths)
        f.write(struct.pack("<Q", 0))  # file_base (0 = directory right after header)
        f.write(b"\x00" * 64)  # reserved

        # File count
        f.write(struct.pack("<I", 1))

        # File entry
        f.write(struct.pack("<I", path_padded_len))
        f.write(path_bytes_padded)
        f.write(struct.pack("<Q", data_offset_aligned))  # offset to file data
        f.write(struct.pack("<Q", len(manifest_data)))  # file size
        f.write(b"\x00" * 16)  # md5 (zeros = skip verification)
        f.write(struct.pack("<I", 0))  # per-file flags

        # Pad to data offset
        current = f.tell()
        if current < data_offset_aligned:
            f.write(b"\x00" * (data_offset_aligned - current))

        # File data
        f.write(manifest_data)

    print(f"Created PCK: {output_path}")


def deploy():
    os.makedirs(MODS_DIR, exist_ok=True)

    # Copy DLL
    dll_src = os.path.join(BUILD_DIR, f"{MOD_NAME}.dll")
    dll_dst = os.path.join(MODS_DIR, f"{MOD_NAME}.dll")
    shutil.copy2(dll_src, dll_dst)
    print(f"Copied DLL to {dll_dst}")

    # Copy System.Speech.dll to mods dir (our assembly resolver loads from there)
    speech_dll = os.path.join(BUILD_DIR, "System.Speech.dll")
    if os.path.exists(speech_dll):
        shutil.copy2(speech_dll, os.path.join(MODS_DIR, "System.Speech.dll"))
        print("Copied System.Speech.dll to mods dir")

    # Create and copy PCK
    pck_path = os.path.join(MODS_DIR, f"{MOD_NAME}.pck")
    create_pck(pck_path)

    print(f"\nMod deployed to {MODS_DIR}")
    print("Start the game to test!")


if __name__ == "__main__":
    build_dll()
    deploy()
