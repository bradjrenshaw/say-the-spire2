using MegaCrit.Sts2.Core.Combat;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("turn", "Turns", category: "Combat")]
public class TurnEvent : GameEvent
{
    private const string BasePath = "events.turn";

    private readonly CombatSide _side;
    private readonly int _round;
    private readonly bool _isStart;

    public TurnEvent(CombatSide side, int round, bool isStart)
    {
        _side = side;
        _round = round;
        _isStart = isStart;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("player_turn_start", "Player Turn Start", true));
        category.Add(new BoolSetting("player_turn_end", "Player Turn End", false));
        category.Add(new BoolSetting("enemy_turn_start", "Enemy Turn Start", true));
        category.Add(new BoolSetting("enemy_turn_end", "Enemy Turn End", false));
        category.Add(new BoolSetting("show_round", "Show Round Number", true));
    }

    private string SubSettingKey
    {
        get
        {
            bool isPlayer = _side == CombatSide.Player;
            return (isPlayer, _isStart) switch
            {
                (true, true) => "player_turn_start",
                (true, false) => "player_turn_end",
                (false, true) => "enemy_turn_start",
                (false, false) => "enemy_turn_end",
            };
        }
    }

    public override bool ShouldAnnounce() =>
        ModSettings.GetValue<bool>($"{BasePath}.{SubSettingKey}");

    public override bool ShouldAddToBuffer() =>
        ModSettings.GetValue<bool>($"{BasePath}.{SubSettingKey}");

    public override Message? GetMessage()
    {
        bool isPlayer = _side == CombatSide.Player;

        if (_isStart)
        {
            if (isPlayer && ModSettings.GetValue<bool>($"{BasePath}.show_round"))
                return Message.Localized("ui", "EVENT.PLAYER_TURN_START", new { round = _round });
            return isPlayer
                ? Message.Localized("ui", "EVENT.PLAYER_TURN")
                : Message.Localized("ui", "EVENT.ENEMY_TURN_START");
        }

        return isPlayer
            ? Message.Localized("ui", "EVENT.PLAYER_TURN_END")
            : Message.Localized("ui", "EVENT.ENEMY_TURN_END");
    }
}
