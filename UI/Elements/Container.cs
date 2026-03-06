using System.Collections.Generic;

namespace SayTheSpire2.UI.Elements;

public abstract class Container : UIElement
{
    private readonly List<UIElement> _children = new();

    public IReadOnlyList<UIElement> Children => _children;
    public string? ContainerLabel { get; set; }
    public bool AnnounceName { get; set; } = true;
    public bool AnnouncePosition { get; set; } = true;

    public override string? GetLabel() => ContainerLabel;

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
    /// Returns a formatted position string for the child element, or null if not applicable.
    /// </summary>
    public override void Update()
    {
        OnUpdate();
        foreach (var child in _children)
            child.Update();
    }

    public abstract string? GetPositionString(UIElement child);
}
