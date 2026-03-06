using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using SayTheSpire2.Localization;
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

        var typeKey = mp.Point.PointType switch
        {
            MapPointType.Unknown => "NODE_TYPES.UNKNOWN",
            MapPointType.Shop => "NODE_TYPES.SHOP",
            MapPointType.Treasure => "NODE_TYPES.TREASURE",
            MapPointType.RestSite => "NODE_TYPES.REST_SITE",
            MapPointType.Monster => "NODE_TYPES.MONSTER",
            MapPointType.Elite => "NODE_TYPES.ELITE",
            MapPointType.Boss => "NODE_TYPES.BOSS",
            MapPointType.Ancient => "NODE_TYPES.ANCIENT",
            _ => "NODE_TYPES.UNKNOWN",
        };
        return LocalizationManager.GetOrDefault("map_nav", typeKey, mp.Point.PointType.ToString());
    }

    public override string? GetTypeKey() => "map node";

    public override string? GetStatusString()
    {
        var mp = MapPointNode;
        if (mp?.Point == null) return null;
        return new LocalizationString("map_nav", "NAV.COORDINATES")
            .Add("col", mp.Point.coord.col.ToString())
            .Add("row", mp.Point.coord.row.ToString())
            .ToString();
    }

    protected override void OnFocus()
    {
        var mp = MapPointNode;
        if (mp?.Point == null) return;

        _mapScreen = new MapScreen(mp.Point);
        ScreenManager.PushScreen(_mapScreen);
    }

    protected override void OnUnfocus()
    {
        if (_mapScreen != null)
        {
            ScreenManager.RemoveScreen(_mapScreen);
            _mapScreen = null;
        }
    }
}
