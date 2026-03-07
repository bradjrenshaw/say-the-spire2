using SayTheSpire2.Settings;

namespace SayTheSpire2.Tests;

public class SettingsTests
{
    private static RootCategorySetting BuildTestTree()
    {
        var root = new RootCategorySetting();
        var speech = new CategorySetting("speech", "Speech");
        speech.Add(new IntSetting("rate", "Rate", defaultValue: 50, min: 0, max: 100));
        speech.Add(new IntSetting("volume", "Volume", defaultValue: 80, min: 0, max: 100));
        speech.Add(new StringSetting("voice", "Voice", "default"));
        root.Add(speech);

        var ui = new CategorySetting("ui", "UI");
        ui.Add(new BoolSetting("verbose", "Verbose mode", false));
        root.Add(ui);

        return root;
    }

    [Fact]
    public void FullPath_TopLevel_ReturnsKey()
    {
        var root = new RootCategorySetting();
        var setting = new BoolSetting("test", "Test");
        root.Add(setting);

        Assert.Equal("test", setting.FullPath);
    }

    [Fact]
    public void FullPath_Nested_ReturnsDotSeparated()
    {
        var root = new RootCategorySetting();
        var cat = new CategorySetting("speech", "Speech");
        var rate = new IntSetting("rate", "Rate");
        root.Add(cat);
        cat.Add(rate);

        Assert.Equal("speech.rate", rate.FullPath);
    }

    [Fact]
    public void FullPath_DeeplyNested()
    {
        var root = new RootCategorySetting();
        var a = new CategorySetting("a", "A");
        var b = new CategorySetting("b", "B");
        var c = new BoolSetting("c", "C");
        root.Add(a);
        a.Add(b);
        b.Add(c);

        Assert.Equal("a.b.c", c.FullPath);
    }

    [Fact]
    public void CategorySetting_GetByKey_FindsChild()
    {
        var cat = new CategorySetting("parent", "Parent");
        var child = new BoolSetting("flag", "Flag");
        cat.Add(child);

        Assert.Same(child, cat.GetByKey("flag"));
    }

    [Fact]
    public void CategorySetting_GetByKey_ReturnsNullForMissing()
    {
        var cat = new CategorySetting("parent", "Parent");
        Assert.Null(cat.GetByKey("nope"));
    }

    [Fact]
    public void CategorySetting_GetTyped_FindsChild()
    {
        var cat = new CategorySetting("parent", "Parent");
        cat.Add(new BoolSetting("flag", "Flag"));
        cat.Add(new IntSetting("count", "Count"));

        var intSetting = cat.Get<IntSetting>("count");
        Assert.NotNull(intSetting);
        Assert.Equal("count", intSetting!.Key);
    }

    [Fact]
    public void BoolSetting_DefaultValue()
    {
        var s = new BoolSetting("test", "Test", true);
        Assert.True(s.Get());
        Assert.True(s.Default);
    }

    [Fact]
    public void BoolSetting_LoadValue()
    {
        var s = new BoolSetting("test", "Test", true);
        s.LoadValue(false);
        Assert.False(s.Get());
    }

    [Fact]
    public void BoolSetting_LoadValue_IgnoresWrongType()
    {
        var s = new BoolSetting("test", "Test", true);
        s.LoadValue("not a bool");
        Assert.True(s.Get());
    }

    [Fact]
    public void IntSetting_Clamps()
    {
        var s = new IntSetting("vol", "Volume", defaultValue: 50, min: 0, max: 100);
        s.LoadValue(150);
        Assert.Equal(100, s.Get());

        s.LoadValue(-10);
        Assert.Equal(0, s.Get());
    }

    [Fact]
    public void IntSetting_LoadsLong()
    {
        var s = new IntSetting("val", "Value", defaultValue: 0, min: 0, max: 100);
        s.LoadValue(42L);
        Assert.Equal(42, s.Get());
    }

    [Fact]
    public void StringSetting_DefaultAndLoad()
    {
        var s = new StringSetting("voice", "Voice", "default");
        Assert.Equal("default", s.Get());

        s.LoadValue("custom");
        Assert.Equal("custom", s.Get());
    }

    [Fact]
    public void StringSetting_LoadValue_IgnoresWrongType()
    {
        var s = new StringSetting("voice", "Voice", "default");
        s.LoadValue(42);
        Assert.Equal("default", s.Get());
    }
}
