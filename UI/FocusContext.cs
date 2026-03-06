using System.Collections.Generic;
using System.Text;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI;

/// <summary>
/// Tracks the container path to the last focused element and diffs against
/// the new path to determine which containers need to be announced.
/// </summary>
public class FocusContext
{
    private List<Container> _lastPath = new();

    /// <summary>
    /// Build the full announcement string for an element, only including
    /// container context that changed since the last focus.
    /// Returns null if there's nothing to announce beyond the element itself.
    /// </summary>
    public string? BuildAnnouncement(UIElement element)
    {
        var newPath = BuildPath(element);
        var divergeIndex = FindDivergenceIndex(_lastPath, newPath);
        _lastPath = newPath;

        var sb = new StringBuilder();

        // Announce containers that changed (from divergence point down)
        for (int i = divergeIndex; i < newPath.Count; i++)
        {
            var container = newPath[i];
            if (container.AnnounceName && !string.IsNullOrEmpty(container.ContainerLabel))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(container.ContainerLabel);
            }
        }

        // Append the element's own focus string
        var focusString = element.GetFocusString();
        if (!string.IsNullOrEmpty(focusString))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(focusString);
        }

        // Append position from immediate parent container
        if (element.Parent is { AnnouncePosition: true } parent)
        {
            var posStr = parent.GetPositionString(element);
            if (!string.IsNullOrEmpty(posStr))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(posStr);
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// Reset the tracked path (e.g., when leaving a screen).
    /// </summary>
    public void Reset()
    {
        _lastPath.Clear();
    }

    private static List<Container> BuildPath(UIElement element)
    {
        var path = new List<Container>();
        var current = element.Parent;
        while (current != null)
        {
            path.Add(current);
            current = current.Parent;
        }
        path.Reverse(); // root first
        return path;
    }

    private static int FindDivergenceIndex(List<Container> oldPath, List<Container> newPath)
    {
        int minLen = System.Math.Min(oldPath.Count, newPath.Count);
        for (int i = 0; i < minLen; i++)
        {
            if (!ReferenceEquals(oldPath[i], newPath[i]))
                return i;
        }
        // If one path is longer, divergence starts at the end of the shorter
        if (newPath.Count > oldPath.Count)
            return oldPath.Count;
        // Paths are identical
        return newPath.Count;
    }
}
