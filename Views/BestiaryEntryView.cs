using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Rooms;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over <see cref="NBestiaryEntry"/>. Centralizes access to the
/// entry's monster/encounter title, discovery state, and room-type qualifier
/// so the rest of the mod doesn't reach into the game node directly.
/// </summary>
public class BestiaryEntryView
{
    public NBestiaryEntry Entry { get; }

    private BestiaryEntryView(NBestiaryEntry entry) { Entry = entry; }

    public static BestiaryEntryView? FromControl(Control? control) =>
        control is NBestiaryEntry entry ? new BestiaryEntryView(entry) : null;

    public BestiaryEntry? Data => Entry.Entry;
    public MonsterModel? Monster => Data?.monsterModel;
    public bool IsUnknown => !Entry.IsDiscovered;
    public bool IsUnderConstruction => Entry.IsUnderConstruction;

    public RoomType MonsterType => Data?.roomType ?? RoomType.Monster;

    /// <summary>
    /// "boss" / "elite" / "monster" — used as the TYPES.* localization key
    /// suffix on the entry's TypeAnnouncement.
    /// </summary>
    public string TypeKey => MonsterType switch
    {
        RoomType.Boss => "boss",
        RoomType.Elite => "elite",
        _ => "monster",
    };

    /// <summary>
    /// The entry's display title — monster name when present, encounter name
    /// otherwise. Mirrors what the game writes into the entry's label.
    /// </summary>
    public string EntryTitle => Data?.GetEntryTitle() ?? "";
}
