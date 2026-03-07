using SayTheSpire2.Settings;

namespace SayTheSpire2.Tests;

[EventSettings("test_combat", "Combat Events")]
public class TestCombatEvent;

[EventSettings("test_dialogue", "Dialogue", defaultBuffer: false)]
public class TestDialogueEvent;

public class EventRegistryTests
{
    [Fact]
    public void Register_CreatesDescriptor()
    {
        var eventsCategory = new CategorySetting("events_a", "Events");
        ModSettings.Root.Add(eventsCategory);
        EventRegistry.Initialize(eventsCategory);

        EventRegistry.Register(typeof(TestCombatEvent));

        Assert.True(EventRegistry.Descriptors.ContainsKey("test_combat"));
        var desc = EventRegistry.Descriptors["test_combat"];
        Assert.Equal("Combat Events", desc.Label);
        Assert.True(desc.DefaultAnnounce);
        Assert.True(desc.DefaultBuffer);
    }

    [Fact]
    public void Register_CreatesSettingsEntries()
    {
        var eventsCategory = new CategorySetting("events_b", "Events");
        ModSettings.Root.Add(eventsCategory);
        EventRegistry.Initialize(eventsCategory);

        EventRegistry.Register(typeof(TestDialogueEvent));

        var announce = ModSettings.GetSetting<BoolSetting>("events_b.test_dialogue.announce");
        var buffer = ModSettings.GetSetting<BoolSetting>("events_b.test_dialogue.buffer");

        Assert.NotNull(announce);
        Assert.NotNull(buffer);
        Assert.True(announce!.Get());
        Assert.False(buffer!.Get());
    }
}
