using System.IO;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Tests;

public class ModSettingsTests : IDisposable
{
    private readonly string _tempDir;

    public ModSettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sts2_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ResolveSetting_FindsNestedSetting()
    {
        var root = ModSettings.Root;
        var cat = new CategorySetting("test_resolve", "Test");
        var child = new BoolSetting("flag", "Flag");
        cat.Add(child);
        root.Add(cat);

        var found = ModSettings.ResolveSetting("test_resolve.flag");
        Assert.Same(child, found);
    }

    [Fact]
    public void ResolveSetting_ReturnsNullForBadPath()
    {
        Assert.Null(ModSettings.ResolveSetting("nonexistent.path.here"));
    }

    [Fact]
    public void GetValue_SetValue_Roundtrip()
    {
        var cat = new CategorySetting("test_getset", "Test");
        cat.Add(new BoolSetting("enabled", "Enabled", true));
        cat.Add(new IntSetting("count", "Count", defaultValue: 5, min: 0, max: 10));
        cat.Add(new StringSetting("name", "Name", "hello"));
        ModSettings.Root.Add(cat);

        Assert.True(ModSettings.GetValue<bool>("test_getset.enabled"));
        Assert.Equal(5, ModSettings.GetValue<int>("test_getset.count"));
        Assert.Equal("hello", ModSettings.GetValue<string>("test_getset.name"));

        ModSettings.SetValue("test_getset.enabled", false);
        ModSettings.SetValue("test_getset.count", 8);
        ModSettings.SetValue("test_getset.name", "world");

        Assert.False(ModSettings.GetValue<bool>("test_getset.enabled"));
        Assert.Equal(8, ModSettings.GetValue<int>("test_getset.count"));
        Assert.Equal("world", ModSettings.GetValue<string>("test_getset.name"));
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        // Set up a fresh root with known settings
        var cat = new CategorySetting("test_persist", "Test");
        cat.Add(new BoolSetting("flag", "Flag", true));
        cat.Add(new IntSetting("num", "Num", defaultValue: 42, min: 0, max: 100));
        cat.Add(new StringSetting("str", "Str", "original"));
        ModSettings.Root.Add(cat);

        // Initialize with temp dir and modify values
        ModSettings.Initialize(_tempDir);
        ModSettings.SetValue("test_persist.flag", false);
        ModSettings.SetValue("test_persist.num", 77);
        ModSettings.SetValue("test_persist.str", "changed");
        ModSettings.Save();

        // Reset values to defaults
        ModSettings.GetSetting<BoolSetting>("test_persist.flag")!.LoadValue(true);
        ModSettings.GetSetting<IntSetting>("test_persist.num")!.LoadValue(0);
        ModSettings.GetSetting<StringSetting>("test_persist.str")!.LoadValue("reset");

        // Load should restore saved values
        ModSettings.Load();

        Assert.False(ModSettings.GetValue<bool>("test_persist.flag"));
        Assert.Equal(77, ModSettings.GetValue<int>("test_persist.num"));
        Assert.Equal("changed", ModSettings.GetValue<string>("test_persist.str"));
    }

    [Fact]
    public void Load_PreservesUnknownKeys()
    {
        // Write a JSON file with an extra key
        var jsonPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(jsonPath, """
        {
            "test_unknown.flag": true,
            "future.setting": "preserved"
        }
        """);

        var cat = new CategorySetting("test_unknown", "Test");
        cat.Add(new BoolSetting("flag", "Flag", false));
        ModSettings.Root.Add(cat);

        ModSettings.Initialize(_tempDir);

        // Known setting loaded
        Assert.True(ModSettings.GetValue<bool>("test_unknown.flag"));

        // Save and verify unknown key is preserved
        ModSettings.Save();
        var saved = File.ReadAllText(jsonPath);
        Assert.Contains("future.setting", saved);
        Assert.Contains("preserved", saved);
    }
}
