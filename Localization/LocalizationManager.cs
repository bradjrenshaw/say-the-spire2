using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Localization;

public static class LocalizationManager
{
    private static readonly Dictionary<string, Dictionary<string, string>> _tables = new();
    private static readonly Dictionary<string, Dictionary<string, string>> _fallbackTables = new();
    private static string _language = "eng";
    private const string LocalizationRoot = "res://SayTheSpire2/localization";

    public static string Language => _language;

    public static void Initialize(string language = "eng")
    {
        _language = language;

        // Always load English as fallback
        LoadLanguageTables("eng", _fallbackTables);

        if (language != "eng")
            LoadLanguageTables(language, _tables);

        Log.Info($"[AccessibilityMod] Localization initialized. Language: {_language}");
    }

    public static void SetLanguage(string language)
    {
        _language = language;
        _tables.Clear();

        if (language != "eng")
            LoadLanguageTables(language, _tables);

        Log.Info($"[AccessibilityMod] Language changed to: {_language}");
    }

    public static string? Get(string table, string key)
    {
        // Try current language first
        if (_language != "eng"
            && _tables.TryGetValue(table, out var langTable)
            && langTable.TryGetValue(key, out var langValue))
        {
            return langValue;
        }

        // Fall back to English
        if (_fallbackTables.TryGetValue(table, out var engTable)
            && engTable.TryGetValue(key, out var engValue))
        {
            return engValue;
        }

        Log.Error($"[AccessibilityMod] Missing localization: {table}.{key}");
        return null;
    }

    public static string GetOrDefault(string table, string key, string defaultValue = "")
    {
        return Get(table, key) ?? defaultValue;
    }

    private static void LoadLanguageTables(string language, Dictionary<string, Dictionary<string, string>> target)
    {
        var langDir = $"{LocalizationRoot}/{language}";

        if (!DirAccess.DirExistsAbsolute(langDir))
        {
            Log.Error($"[AccessibilityMod] Localization directory not found: {langDir}");
            return;
        }

        using var dir = DirAccess.Open(langDir);
        if (dir == null)
        {
            Log.Error($"[AccessibilityMod] Could not open localization directory: {langDir}");
            return;
        }

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (!fileName.EndsWith(".json"))
                continue;

            var tableName = fileName[..^5]; // strip .json
            var filePath = $"{langDir}/{fileName}";

            try
            {
                using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    Log.Error($"[AccessibilityMod] Could not open {filePath}");
                    continue;
                }

                var json = file.GetAsText();
                var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (entries != null)
                {
                    target[tableName] = entries;
                    Log.Info($"[AccessibilityMod] Loaded localization table: {language}/{tableName} ({entries.Count} entries)");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AccessibilityMod] Failed to load {filePath}: {ex.Message}");
            }
        }
        dir.ListDirEnd();
    }
}
