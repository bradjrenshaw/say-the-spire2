namespace SayTheSpire2.UI.Elements;

public class ListContainer : Container
{
    public override string? GetPositionString(UIElement child)
    {
        var idx = IndexOf(child);
        if (idx < 0) return null;
        return $"{idx + 1} of {Children.Count}";
    }
}
