using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("enemy_move", "Enemy Moves", category: "Combat", hasSourceFilter: true, allowCurrentPlayer: false, allowOtherPlayers: false)]
public class EnemyMoveEvent : GameEvent
{
    private readonly string _creatureName;
    private readonly string _intentSummary;

    public EnemyMoveEvent(Creature creature, string intentSummary)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
        _intentSummary = intentSummary;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("show_intent", "Show Intent on Enemy Move", false, localizationKey: "EVENTS.ENEMY_MOVE.SHOW_INTENT"));
    }

    public override Message? GetMessage()
    {
        if (ModSettings.GetValue<bool>("events.enemy_move.show_intent") && !string.IsNullOrEmpty(_intentSummary))
            return Message.Localized("ui", "EVENT.ENEMY_MOVE", new { creature = _creatureName, intent = _intentSummary });
        return Message.Raw(_creatureName);
    }
}
