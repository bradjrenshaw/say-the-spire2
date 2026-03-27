using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using SayTheSpire2.Map;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI.Elements;

public class ProxyMapPoint : ProxyElement
{
    private MapScreen? _mapScreen;

    public ProxyMapPoint(Control control) : base(control) { }

    private NMapPoint? MapPointNode => Control as NMapPoint;

    public override string? GetLabel()
    {
        var mp = MapPointNode;
        if (mp?.Point == null) return CleanNodeName(Control.Name);
        return MapScreen.Current?.DescribePoint(mp.Point, includeChoicePrefix: false)
            ?? MapNode.GetPointDisplayName(mp.Point);
    }

    public override string? GetTypeKey() => null;

    public override string? GetStatusString()
    {
        return null;
    }

    protected override void OnFocus()
    {
        var mp = MapPointNode;
        if (mp?.Point == null) return;

        // If a MapScreen already exists (e.g. from the map key hook), update its start point
        if (MapScreen.Current != null)
        {
            MapScreen.Current.UpdateStartPoint(mp.Point);
            return;
        }

        _mapScreen = new MapScreen(mp.Point);
        ScreenManager.PushScreen(_mapScreen);
    }

    protected override void OnUnfocus()
    {
        // Only remove the screen if we created it
        if (_mapScreen != null)
        {
            ScreenManager.RemoveScreen(_mapScreen);
            _mapScreen = null;
        }
    }
}
