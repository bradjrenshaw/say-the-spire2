using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ListContainer : Container
{
    public override Message? GetPositionString(UIElement child)
    {
        int position = 0;
        int total = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].IsVisible) continue;
            total++;
            if (Children[i] == child)
                position = total;
        }
        if (position == 0) return null;
        return Message.Localized("ui", "POSITIONS.LIST", new { position, total });
    }
}
