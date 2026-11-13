using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

/// <summary>
/// The local player gained a melody (the Fool's Wah / Cho / Ki): the game
/// adds its symbol to the bow. Reads like a power gain, "The Fool gained Wah
/// Melody". Melodies all drop at the end of the turn, which isn't announced.
/// </summary>
[EventSettings("melody", "Melodies", category: "Combat", hasSourceFilter: true, allowOtherPlayers: false, allowEnemies: false)]
public class MelodyEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly string _melodyTitle;

    public MelodyEvent(Creature creature, string melodyTitle)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _melodyTitle = melodyTitle;
    }

    public override Message? GetMessage() =>
        Message.Localized("ui", "EVENT.POWER_GAINED_NO_AMOUNT", new { creature = _creatureName, power = _melodyTitle });
}
