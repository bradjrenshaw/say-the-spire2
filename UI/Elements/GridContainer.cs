using System.Collections.Generic;

namespace SayTheSpire2.UI.Elements;

public class GridContainer : Container
{
    private readonly Dictionary<UIElement, (int row, int col)> _positions = new();

    public int Rows { get; private set; }
    public int Columns { get; private set; }

    public void Add(UIElement child, int row, int col)
    {
        base.Add(child);
        _positions[child] = (row, col);
        if (row >= Rows) Rows = row + 1;
        if (col >= Columns) Columns = col + 1;
    }

    public override string? GetPositionString(UIElement child)
    {
        if (!_positions.TryGetValue(child, out var pos)) return null;
        return $"{pos.row + 1}, {pos.col + 1}";
    }
}
