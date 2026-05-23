using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A single addressable piece of an element's spoken focus message. One class
/// per semantic concept (HpAnnouncement, IntentsAnnouncement, LabelAnnouncement,
/// ...) — the class owns the rendering logic for that concept and is reused
/// everywhere that concept appears.
///
/// <para>The same Announcement renders in two contexts:</para>
/// <list type="bullet">
/// <item><see cref="Render"/> — the focus context. Returns a single Message
/// that the focus composer comma-joins with siblings into one spoken
/// sentence.</item>
/// <item><see cref="RenderBuffer"/> — the buffer context. Returns 1+ Messages
/// that the buffer composer writes as separate browsable entries. Default
/// implementation yields the focus Render result so most announcements need
/// no override; multi-line entries (hover tips groups, extras) override.</item>
/// </list>
/// </summary>
public abstract class Announcement
{
    /// <summary>
    /// Stable string identity for this announcement type (e.g., "hp", "label",
    /// "intents"). Used for settings paths and introspection.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// The announcement's rendered text as a Message. The context gives access
    /// to per-element-resolved setting values (verbose toggles, thresholds, etc.)
    /// — announcements that don't declare custom settings can ignore the param.
    /// </summary>
    public abstract Message Render(AnnouncementContext ctx);

    /// <summary>
    /// The announcement's rendered text as one-or-more buffer entries. Each
    /// yielded Message becomes a separate browsable buffer line. Default
    /// behavior is to yield the focus Render result so simple announcements
    /// inherit reasonable behavior; override when the buffer wants different
    /// wording or multiple lines.
    /// </summary>
    public virtual IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        yield return Render(ctx);
    }

    /// <summary>
    /// Renders to a single spoken utterance for the hotkey context — the
    /// <see cref="RenderBuffer"/> lines joined with <paramref name="separator"/>.
    /// Single-value announcements (HP, gold) yield one line and so produce
    /// that value; group announcements (powers, intents) produce the joined
    /// list. Returns <see cref="Message.Empty"/> when there's nothing to say,
    /// letting the caller substitute a context-specific "nothing" message.
    /// </summary>
    public Message RenderJoined(AnnouncementContext ctx, string separator = ", ")
    {
        var parts = new List<Message>();
        foreach (var msg in RenderBuffer(ctx))
        {
            if (!string.IsNullOrEmpty(msg?.Resolve()))
                parts.Add(msg);
        }
        return parts.Count == 0 ? Message.Empty : Message.Join(separator, parts.ToArray());
    }

    /// <summary>
    /// Punctuation appended to this announcement's rendered text before the
    /// composer space-joins it with the next announcement. Default is empty
    /// (pure space-join). Subclasses that want a comma after them override this.
    /// </summary>
    public virtual string Suffix => "";
}
