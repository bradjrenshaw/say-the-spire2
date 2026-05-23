using System;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Declares the canonical order in which announcements appear when this
/// <see cref="Buffers.Buffer"/> is populated. Mirrors <see cref="AnnouncementOrderAttribute"/>
/// for the buffer context: the buffer composer renders the listed announcement
/// types in this order, yielded-but-not-listed entries get appended at the end.
/// Used by the registry to materialize the per-buffer settings tree at
/// <c>buffers.{buffer_key}.announcements.{ann_key}/</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class BufferAnnouncementOrderAttribute : Attribute
{
    public Type[] Types { get; }

    public BufferAnnouncementOrderAttribute(params Type[] types)
    {
        Types = types;
    }
}
