using MegaCrit.Sts2.Core.Entities.Creatures;

namespace SayTheSpire2.Events;

public abstract class GameEvent
{
    /// <summary>
    /// The creature this event applies to (player, other player, or enemy).
    /// Null for events without a creature source (dialogue, room entered, etc.)
    /// </summary>
    public Creature? Source { get; set; }

    public abstract SayTheSpire2.Localization.Message? GetMessage();

    public virtual bool ShouldAnnounce() => true;

    public virtual bool ShouldAddToBuffer() => true;
}
