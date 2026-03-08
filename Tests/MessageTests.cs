using SayTheSpire2.Localization;

namespace SayTheSpire2.Tests;

public class MessageTests : IDisposable
{
    public MessageTests()
    {
        // Set up a test localization resolver
        Message.LocalizationResolver = (table, key) =>
        {
            var entries = new Dictionary<string, Dictionary<string, string>>
            {
                ["ui"] = new()
                {
                    ["CARD.COST"] = "Costs {cost} energy",
                    ["GREETING"] = "Hello, {name}!",
                    ["PLAIN"] = "No variables here",
                    ["WITH_BBCODE"] = "[b]Bold[/b] and [img]res://icons/energy_icon.png[/img]",
                },
            };
            if (entries.TryGetValue(table, out var t) && t.TryGetValue(key, out var v))
                return v;
            return null;
        };
    }

    public void Dispose()
    {
        Message.LocalizationResolver = null;
    }

    [Fact]
    public void StripBbcode_RemovesTags()
    {
        var result = Message.StripBbcode("[b]bold text[/b]");
        Assert.Equal("bold text", result);
    }

    [Fact]
    public void StripBbcode_RemovesNestedTags()
    {
        var result = Message.StripBbcode("[b][i]bold italic[/i][/b]");
        Assert.Equal("bold italic", result);
    }

    [Fact]
    public void StripBbcode_ResolvesImgTags()
    {
        var result = Message.StripBbcode("[img]res://images/icons/energy_icon.png[/img]");
        Assert.Equal("Energy", result);
    }

    [Fact]
    public void StripBbcode_ResolvesStrayResPaths()
    {
        var result = Message.StripBbcode("Cost: res://images/icons/gold_icon.png 50");
        Assert.Equal("Cost: Gold 50", result);
    }

    [Fact]
    public void StripBbcode_UnknownIconFallsBackToCleanName()
    {
        var result = Message.StripBbcode("[img]res://images/unknown_thing.png[/img]");
        Assert.Equal("unknown thing", result);
    }

    [Fact]
    public void StripBbcode_MixedContent()
    {
        var result = Message.StripBbcode("Deal [b]7[/b] damage. Costs [img]res://icons/energy_icon.png[/img] 2.");
        Assert.Equal("Deal 7 damage. Costs Energy 2.", result);
    }

    [Fact]
    public void StripBbcode_PlainTextPassesThrough()
    {
        var result = Message.StripBbcode("plain text");
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void StripBbcode_TrimsWhitespace()
    {
        var result = Message.StripBbcode("  [b]text[/b]  ");
        Assert.Equal("text", result);
    }

    [Fact]
    public void SubstituteVars_ReplacesVariables()
    {
        var vars = new Dictionary<string, string> { ["name"] = "Strike", ["cost"] = "1" };
        var result = Message.SubstituteVars("{name} costs {cost} energy", vars);
        Assert.Equal("Strike costs 1 energy", result);
    }

    [Fact]
    public void SubstituteVars_LeavesUnknownVarsIntact()
    {
        var vars = new Dictionary<string, string> { ["name"] = "Strike" };
        var result = Message.SubstituteVars("{name} costs {cost} energy", vars);
        Assert.Equal("Strike costs {cost} energy", result);
    }

    [Fact]
    public void SubstituteVars_NoVarsReturnsOriginal()
    {
        var vars = new Dictionary<string, string>();
        var result = Message.SubstituteVars("plain text", vars);
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void ResolveIconPath_MatchesKnownSuffix()
    {
        var result = Message.ResolveIconPath("res://images/ironclad_energy_icon.png");
        Assert.Equal("Energy", result);
    }

    [Fact]
    public void ResolveIconPath_ExactMatch()
    {
        var result = Message.ResolveIconPath("res://icons/gold_icon.png");
        Assert.Equal("Gold", result);
    }

    [Fact]
    public void ResolveIconPath_UnknownReturnsCleanedName()
    {
        var result = Message.ResolveIconPath("res://images/some_fancy_widget.png");
        Assert.Equal("some fancy widget", result);
    }

    [Fact]
    public void Raw_ResolvesWithBbcodeStripping()
    {
        var msg = Message.Raw("[b]bold[/b] text");
        Assert.Equal("bold text", msg.Resolve());
    }

    [Fact]
    public void Raw_WithAnonymousObjectVars()
    {
        var msg = Message.Raw("Deal {damage} damage", new { damage = 7 });
        Assert.Equal("Deal 7 damage", msg.Resolve());
    }

    [Fact]
    public void Raw_WithDictionaryVars()
    {
        var vars = new Dictionary<string, string> { ["damage"] = "7" };
        var msg = Message.Raw("Deal {damage} damage", vars);
        Assert.Equal("Deal 7 damage", msg.Resolve());
    }

    [Fact]
    public void Raw_VarsAndBbcodeStripping()
    {
        var msg = Message.Raw("[b]{name}[/b] costs [img]res://icons/energy_icon.png[/img] {cost}", new { name = "Strike", cost = 1 });
        Assert.Equal("Strike costs Energy 1", msg.Resolve());
    }

    [Fact]
    public void ToString_CallsResolve()
    {
        var msg = Message.Raw("hello");
        Assert.Equal("hello", msg.ToString());
    }

    [Fact]
    public void Localized_ResolvesFromResolver()
    {
        var msg = Message.Localized("ui", "PLAIN");
        Assert.Equal("No variables here", msg.Resolve());
    }

    [Fact]
    public void Localized_WithAnonymousObjectVars()
    {
        var msg = Message.Localized("ui", "CARD.COST", new { cost = 3 });
        Assert.Equal("Costs 3 energy", msg.Resolve());
    }

    [Fact]
    public void Localized_WithDictionaryVars()
    {
        var vars = new Dictionary<string, string> { ["name"] = "World" };
        var msg = Message.Localized("ui", "GREETING", vars);
        Assert.Equal("Hello, World!", msg.Resolve());
    }

    [Fact]
    public void Localized_StripsBbcode()
    {
        var msg = Message.Localized("ui", "WITH_BBCODE");
        Assert.Equal("Bold and Energy", msg.Resolve());
    }

    [Fact]
    public void Localized_MissingKeyReturnsFallback()
    {
        var msg = Message.Localized("ui", "NONEXISTENT");
        Assert.Equal("MISSING(ui.NONEXISTENT)", msg.Resolve());
    }

    [Fact]
    public void Localized_MissingTableReturnsFallback()
    {
        var msg = Message.Localized("missing", "KEY");
        Assert.Equal("MISSING(missing.KEY)", msg.Resolve());
    }
}
