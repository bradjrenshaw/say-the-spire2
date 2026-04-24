using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public abstract class Container : UIElement
{
    private readonly List<UIElement> _children = new();

    public IReadOnlyList<UIElement> Children => _children;
    public string? ContainerLabel { get; set; }
    public bool AnnounceName { get; set; } = true;
    public bool AnnouncePosition { get; set; } = true;

    public override Message? GetLabel() => ContainerLabel != null ? Message.Raw(ContainerLabel) : null;

    /// <summary>
    /// Containers aren't focused directly — their label is read via FocusContext
    /// path diffing when a child is focused. Default is empty; subclasses can
    /// override if they want a direct-focus announcement.
    /// </summary>
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield break;
    }

    public void Add(UIElement child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    public void Remove(UIElement child)
    {
        if (_children.Remove(child))
            child.Parent = null;
    }

    public void Clear()
    {
        foreach (var child in _children)
            child.Parent = null;
        _children.Clear();
    }

    public int IndexOf(UIElement child) => _children.IndexOf(child);

    /// <summary>
    /// Recursively detaches every descendant, then self. Containers call
    /// children's Detach so a screen can tear the whole element tree down by
    /// invoking Detach on the root.
    /// </summary>
    public override void Detach()
    {
        foreach (var child in _children)
            child.Detach();
        base.Detach();
    }

    /// <summary>
    /// Swap two children's positions in this container. No-op for invalid indices.
    /// </summary>
    public void Swap(int a, int b)
    {
        if (a == b) return;
        if (a < 0 || a >= _children.Count) return;
        if (b < 0 || b >= _children.Count) return;
        (_children[a], _children[b]) = (_children[b], _children[a]);
    }

    /// <summary>
    /// Returns a formatted position string for the child element, or null if not applicable.
    /// </summary>
    public override void Update()
    {
        OnUpdate();
        foreach (var child in _children)
            child.Update();
    }

    public abstract Message? GetPositionString(UIElement child);
}
