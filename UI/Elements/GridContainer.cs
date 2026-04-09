using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class GridContainer : Container
{
    private readonly Dictionary<UIElement, (int x, int y)> _positions = new();

    public int MaxX { get; private set; }
    public int MaxY { get; private set; }

    public void Add(UIElement child, int x, int y)
    {
        base.Add(child);
        _positions[child] = (x, y);
        if (x >= MaxX) MaxX = x + 1;
        if (y >= MaxY) MaxY = y + 1;
    }

    public void ClearGrid()
    {
        base.Clear();
        _positions.Clear();
        MaxX = 0;
        MaxY = 0;
    }

    public override Message? GetPositionString(UIElement child)
    {
        if (!_positions.TryGetValue(child, out var pos)) return null;
        return Message.Localized("ui", "POSITIONS.GRID", new { row = pos.y + 1, column = pos.x + 1 });
    }
}
