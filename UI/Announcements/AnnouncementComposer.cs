using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Turns an element's yielded announcements into a final spoken focus message.
/// Reads the element's [AnnouncementOrder] attribute, sorts yielded announcements
/// by that order (undeclared ones appended at the end in yield order), renders
/// each, and joins them. Each announcement's Suffix sits between it and the next
/// announcement, so the last announcement's suffix is intentionally dropped —
/// no trailing punctuation.
/// </summary>
public static class AnnouncementComposer
{
    public static Message Compose(UIElement element, IEnumerable<Announcement> announcements)
    {
        var order = element.AnnouncementOrderType.GetCustomAttribute<AnnouncementOrderAttribute>()?.Types
            ?? Array.Empty<Type>();

        // Partition into declared (keyed by type) and undeclared (kept in yield order)
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

        // Emit declared in attribute order, then undeclared in yield order
        var sorted = new List<Announcement>(declared.Count + undeclared.Count);
        foreach (var t in order)
        {
            if (declared.TryGetValue(t, out var a))
                sorted.Add(a);
        }
        sorted.AddRange(undeclared);

        // Render, skip disabled (per-element override, else global), skip empty
        var rendered = new List<(string Text, string Suffix)>();
        foreach (var a in sorted)
        {
            if (!IsEnabled(element, a.Key)) continue;
            var text = a.Render()?.Resolve();
            if (!string.IsNullOrEmpty(text))
                rendered.Add((text, a.Suffix));
        }

        if (rendered.Count == 0) return Message.Empty;

        // Join: each announcement's suffix sits between it and the next, space-separated.
        // The last announcement's suffix is dropped (no trailing punctuation).
        var sb = new StringBuilder();
        for (int i = 0; i < rendered.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(rendered[i - 1].Suffix);
                sb.Append(' ');
            }
            sb.Append(rendered[i].Text);
        }

        return Message.Raw(sb.ToString());
    }

    /// <summary>
    /// Whether the announcement should be emitted for this element. Checks the
    /// per-element override (NullableBoolSetting) first; falls back to the global
    /// enabled flag. Returns true if neither is registered (covers announcements
    /// added after startup or test environments without a populated settings tree).
    /// </summary>
    private static bool IsEnabled(UIElement element, string announcementKey)
    {
        var elementKey = AnnouncementRegistry.DeriveElementKey(element.AnnouncementOrderType);
        var overrideSetting = ModSettings.GetSetting<NullableBoolSetting>(
            $"ui.{elementKey}.announcements.{announcementKey}.enabled");
        if (overrideSetting != null)
            return overrideSetting.Resolved;

        var global = ModSettings.GetSetting<BoolSetting>($"announcements.{announcementKey}.enabled");
        return global?.Value ?? true;
    }
}
