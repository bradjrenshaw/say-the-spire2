using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SayTheSpire2.Settings;

public static class ModSettings
{
    public static RootCategorySetting Root { get; } = new();

    private static bool _dirty;
    private static string? _settingsPath;
    private static Dictionary<string, JsonElement>? _unknownKeys;

    public static void Initialize(string settingsDir)
    {
        Directory.CreateDirectory(settingsDir);
        _settingsPath = Path.Combine(settingsDir, "settings.json");
        Load();
        Save();
    }

    public static void MarkDirty()
    {
        _dirty = true;
        SaveIfDirty();
    }

    public static void SaveIfDirty()
    {
        if (_dirty)
            Save();
    }

    // -- Convenience accessors by dot-separated path --

    public static T GetValue<T>(string path)
    {
        var setting = ResolveSetting(path);
        return setting switch
        {
            BoolSetting b when typeof(T) == typeof(bool) => (T)(object)b.Get(),
            StringSetting s when typeof(T) == typeof(string) => (T)(object)s.Get(),
            IntSetting i when typeof(T) == typeof(int) => (T)(object)i.Get(),
            _ => throw new InvalidOperationException(
                $"Setting '{path}' not found or type mismatch (expected {typeof(T).Name})")
        };
    }

    public static void SetValue<T>(string path, T value)
    {
        var setting = ResolveSetting(path);
        switch (setting)
        {
            case BoolSetting b when value is bool bv:
                b.Set(bv);
                break;
            case StringSetting s when value is string sv:
                s.Set(sv);
                break;
            case IntSetting i when value is int iv:
                i.Set(iv);
                break;
            default:
                throw new InvalidOperationException(
                    $"Setting '{path}' not found or type mismatch");
        }
    }

    public static T? GetSetting<T>(string path) where T : Setting
    {
        return ResolveSetting(path) as T;
    }

    // -- Serialization --

    public static void Load()
    {
        if (_settingsPath == null || !File.Exists(_settingsPath))
            return;

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (doc == null) return;

            _unknownKeys = new Dictionary<string, JsonElement>();

            foreach (var (key, element) in doc)
            {
                var setting = ResolveSetting(key);
                if (setting == null)
                {
                    _unknownKeys[key] = element;
                    continue;
                }

                setting.LoadValue(DeserializeElement(element));
            }
        }
        catch (Exception)
        {
            // Corrupt file — use defaults, don't overwrite
            // Callers with logging can catch and log separately
        }
    }

    public static void Save()
    {
        if (_settingsPath == null) return;

        try
        {
            var dict = new Dictionary<string, object?>();

            // Collect all leaf values from the tree
            CollectValues(Root, dict);

            // Preserve unknown keys from the loaded file
            if (_unknownKeys != null)
            {
                foreach (var (key, element) in _unknownKeys)
                    dict[key] = element;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(dict, options);
            File.WriteAllText(_settingsPath, json);
            _dirty = false;
        }
        catch (Exception)
        {
            // Save failed — leave dirty flag set so we retry later
        }
    }

    // -- Internal helpers --

    internal static Setting? ResolveSetting(string path)
    {
        var parts = path.Split('.');
        Setting current = Root;

        foreach (var part in parts)
        {
            if (current is CategorySetting cat)
            {
                var child = ResolveChild(cat, part);
                if (child == null) return null;
                current = child;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private static Setting? ResolveChild(CategorySetting category, string key)
    {
        var direct = category.GetByKey(key);
        if (direct != null)
            return direct;

        foreach (var virtualCategory in category.Children.OfType<CategorySetting>())
        {
            if (virtualCategory.IncludeInPath)
                continue;

            var found = ResolveChild(virtualCategory, key);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void CollectValues(Setting setting, Dictionary<string, object?> dict)
    {
        if (setting is CategorySetting cat)
        {
            foreach (var child in cat.Children)
                CollectValues(child, dict);
        }
        else
        {
            dict[setting.FullPath] = setting.BoxedValue;
        }
    }

    private static object? DeserializeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Array => element,
            _ => null
        };
    }
}
