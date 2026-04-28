using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The card-level modifiers — Replay count, Enchantment, Affliction — rendered
/// as a single parenthesized clause that follows the card's label, e.g.
/// "Strike (Replay 5, Sharp, Hexed)". Each part is independently togglable
/// via per-element / global settings; if all three are disabled or empty,
/// the announcement renders nothing and contributes no punctuation.
/// </summary>
[ShowInGlobalSettings]
public sealed class ModifiersAnnouncement : Announcement
{
    private readonly int _replayCount;
    private readonly string? _enchantmentTitle;
    private readonly string? _afflictionTitle;

    public ModifiersAnnouncement(int replayCount, string? enchantmentTitle, string? afflictionTitle)
    {
        _replayCount = replayCount;
        _enchantmentTitle = enchantmentTitle;
        _afflictionTitle = afflictionTitle;
    }

    public override string Key => "modifiers";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("show_replay", "Show Replay", true,
            localizationKey: "SETTINGS.MODIFIERS.SHOW_REPLAY"));
        category.Add(new BoolSetting("show_enchantment", "Show Enchantment", true,
            localizationKey: "SETTINGS.MODIFIERS.SHOW_ENCHANTMENT"));
        category.Add(new BoolSetting("show_affliction", "Show Affliction", true,
            localizationKey: "SETTINGS.MODIFIERS.SHOW_AFFLICTION"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var parts = new List<Message>();

        if (_replayCount > 0 && ctx.ResolveBool(Key, "show_replay", true))
            parts.Add(Message.Localized("ui", "MODIFIER.REPLAY", new { count = _replayCount }));

        if (!string.IsNullOrEmpty(_enchantmentTitle) && ctx.ResolveBool(Key, "show_enchantment", true))
            parts.Add(Message.Raw(_enchantmentTitle));

        if (!string.IsNullOrEmpty(_afflictionTitle) && ctx.ResolveBool(Key, "show_affliction", true))
            parts.Add(Message.Raw(_afflictionTitle));

        if (parts.Count == 0) return Message.Empty;
        return Message.Raw("(") + Message.Join(", ", parts.ToArray()) + Message.Raw(")");
    }
}
