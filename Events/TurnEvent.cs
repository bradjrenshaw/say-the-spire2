using MegaCrit.Sts2.Core.Combat;

namespace Sts2AccessibilityMod.Events;

public class TurnEvent : GameEvent
{
    private readonly CombatSide _side;
    private readonly int _round;
    private readonly bool _isStart;

    public TurnEvent(CombatSide side, int round, bool isStart)
    {
        _side = side;
        _round = round;
        _isStart = isStart;
    }

    public override string? GetMessage()
    {
        if (_isStart)
        {
            return _side == CombatSide.Player
                ? $"Your turn, round {_round}"
                : "Enemy turn";
        }
        return null;
    }

    public override bool ShouldAddToBuffer() => _isStart;
}
