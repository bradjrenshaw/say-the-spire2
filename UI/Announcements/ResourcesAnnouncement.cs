using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Combined player resources line — energy plus stars (when present), joined
/// with commas. The card focus path reuses <see cref="EnergyAnnouncement"/>
/// and <see cref="StarsAnnouncement"/> separately so the user can reorder
/// them; the player buffer and the Ctrl+Y hotkey want the resources to
/// share one browsable / spoken entry.
///
/// <para>Honors a "verbose" setting (default true) that cascades per-buffer
/// / global:</para>
/// <list type="bullet">
/// <item>Verbose: "3/3 energy, 2 stars"</item>
/// <item>Compact: "3/3, 2"</item>
/// </list>
/// </summary>
[ShowInGlobalSettings]
public sealed class ResourcesAnnouncement : Announcement
{
    private readonly PlayerCombatState _pcs;

    public ResourcesAnnouncement(PlayerCombatState pcs) { _pcs = pcs; }

    public override string Key => "resources";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        var parts = new List<Message>
        {
            verbose
                ? Message.Localized("ui", "RESOURCE.ENERGY", new { current = _pcs.Energy, max = _pcs.MaxEnergy })
                : Message.Localized("ui", "RESOURCE.ENERGY_COMPACT", new { current = _pcs.Energy, max = _pcs.MaxEnergy })
        };
        if (_pcs.Stars > 0)
        {
            parts.Add(verbose
                ? Message.Localized("ui", "RESOURCE.STARS", new { amount = _pcs.Stars })
                : Message.Localized("ui", "RESOURCE.STARS_COMPACT", new { amount = _pcs.Stars }));
        }
        return Message.Join(", ", parts.ToArray());
    }
}
