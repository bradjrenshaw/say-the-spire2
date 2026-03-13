using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Settings;

/// <summary>
/// Reads installation.json from the mod settings directory.
/// This file is managed by the installer and the Ctrl+Shift+A toggle.
/// The mod treats it as read-only at startup (except for the toggle hotkey).
/// </summary>
public static class InstallationConfig
{
    public static bool ScreenReader { get; private set; } = true;
    public static bool DisableGodotUia { get; private set; } = true;

    private static string? _installationPath;
    private static string? _legacyAccessibilityPath;

    public static void Initialize(string settingsDir)
    {
        _installationPath = Path.Combine(settingsDir, "installation.json");
        _legacyAccessibilityPath = Path.Combine(settingsDir, "accessibility.json");

        if (File.Exists(_installationPath))
        {
            ReadInstallationJson();
        }
        else if (File.Exists(_legacyAccessibilityPath))
        {
            // Migrate from old accessibility.json
            MigrateFromAccessibilityJson();
        }
        // else: no file exists, defaults (both true) are fine
    }

    private static void ReadInstallationJson()
    {
        try
        {
            var json = File.ReadAllText(_installationPath!);
            var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (doc == null) return;

            if (doc.TryGetValue("screen_reader", out var sr))
                ScreenReader = sr.ValueKind != JsonValueKind.False;
            if (doc.TryGetValue("disable_godot_uia", out var uia))
                DisableGodotUia = uia.ValueKind != JsonValueKind.False;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to read installation.json: {e.Message}");
        }
    }

    private static void MigrateFromAccessibilityJson()
    {
        try
        {
            var json = File.ReadAllText(_legacyAccessibilityPath!);
            var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (doc != null && doc.TryGetValue("enabled", out var val) && val.ValueKind == JsonValueKind.False)
                ScreenReader = false;

            Log.Info("[AccessibilityMod] Migrated from accessibility.json to installation.json defaults.");
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to read legacy accessibility.json: {e.Message}");
        }
    }

    public static void SetScreenReader(bool enabled)
    {
        ScreenReader = enabled;
        Save();
    }

    private static void Save()
    {
        try
        {
            if (_installationPath == null) return;
            var obj = new { screen_reader = ScreenReader, disable_godot_uia = DisableGodotUia };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_installationPath, json);
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to write installation.json: {e.Message}");
        }
    }
}
