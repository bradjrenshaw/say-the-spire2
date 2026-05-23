using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SayTheSpire2.Buffers;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context analog of <see cref="AnnouncementComposer"/>. Reads the
/// <see cref="BufferAnnouncementOrderAttribute"/> declared on the Buffer
/// subclass (or supplied directly), respects the user's per-buffer
/// reorder/enable settings, and writes each enabled announcement's
/// <see cref="Announcement.RenderBuffer"/> output as one or more buffer
/// entries via <see cref="Buffer.Add"/>.
///
/// <para>Unlike the focus composer which produces a single comma-joined
/// sentence, the buffer composer keeps each announcement's lines as separate
/// browsable entries — exactly what the user navigates with the buffer
/// review controls. Multi-line announcements (hover tips) get their lines
/// added consecutively at the announcement's slot in the order.</para>
/// </summary>
public static class BufferAnnouncementComposer
{
    /// <summary>
    /// Compose the announcements into <paramref name="buffer"/> using the
    /// order declared on the buffer's type via
    /// <see cref="BufferAnnouncementOrderAttribute"/>.
    /// </summary>
    public static void Compose(SayTheSpire2.Buffers.Buffer buffer, IEnumerable<Announcement> announcements)
    {
        var attrOrder = buffer.GetType().GetCustomAttribute<BufferAnnouncementOrderAttribute>()?.Types
            ?? Array.Empty<Type>();
        Compose(buffer, buffer.Key, attrOrder, announcements);
    }

    /// <summary>
    /// Compose the announcements into <paramref name="buffer"/> using an
    /// explicit settings key and attribute order. Used by buffers that share
    /// a settings tree (e.g. UpgradeBuffer rendering card content under the
    /// "card" settings scope).
    /// </summary>
    public static void Compose(SayTheSpire2.Buffers.Buffer buffer, string settingsKey, Type[] attrOrder,
        IEnumerable<Announcement> announcements)
    {
        var ctx = AnnouncementContext.ForBuffer(settingsKey);
        var order = ResolveUserOrder(settingsKey, attrOrder);

        // Partition into declared (keyed by type) and undeclared
        // (kept in yield order, appended after the declared slots).
        var declared = new Dictionary<Type, Announcement>();
        var undeclared = new List<Announcement>();
        foreach (var a in announcements)
        {
            var t = a.GetType();
            if (order.Contains(t) && !declared.ContainsKey(t))
                declared[t] = a;
            else
                undeclared.Add(a);
        }

        var sorted = new List<Announcement>(declared.Count + undeclared.Count);
        foreach (var t in order)
        {
            if (declared.TryGetValue(t, out var a))
                sorted.Add(a);
        }
        sorted.AddRange(undeclared);

        foreach (var a in sorted)
        {
            if (!ctx.ResolveBool(a.Key, "enabled", true)) continue;
            foreach (var msg in a.RenderBuffer(ctx))
            {
                var text = msg?.Resolve();
                if (!string.IsNullOrEmpty(text))
                    buffer.Add(text);
            }
        }
    }

    private static Type[] ResolveUserOrder(string settingsKey, Type[] attrOrder)
    {
        var orderSetting = ModSettings.GetSetting<StringSetting>($"buffers.{settingsKey}.announcements.order");
        return AnnouncementRegistry.MergeUserOrderWithAttrOrder(orderSetting?.Value, attrOrder).ToArray();
    }
}
