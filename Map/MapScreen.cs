using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Input;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Map;

public class MapScreen : Screen
{
    private readonly MapHandler _handler = new();
    private TreeMapViewer? _viewer;
    private MapPoint? _startPoint;

    public MapScreen(MapPoint startPoint)
    {
        _startPoint = startPoint;

        // Claim the Ctrl+Arrow actions — on the map these become navigation
        // instead of buffer controls. Not propagated, so DefaultScreen won't see them.
        ClaimAction("buffer_next_item"); // Ctrl+Up -> forward (toward boss)
        ClaimAction("buffer_prev_item"); // Ctrl+Down -> backward (toward start)
        ClaimAction("buffer_next");      // Ctrl+Right -> next branch
        ClaimAction("buffer_prev");      // Ctrl+Left -> prev branch
    }

    public override void OnPush()
    {
        if (!_handler.Build())
        {
            Log.Error("[AccessibilityMod] MapScreen: Failed to build map graph");
            return;
        }

        _viewer = new TreeMapViewer(_handler);

        if (_startPoint != null)
        {
            var startNode = _handler.GetNode(_startPoint);
            if (startNode != null)
                _viewer.SetStartNode(startNode);
        }

        Log.Info("[AccessibilityMod] MapScreen pushed, viewer ready");
    }

    public override void OnPop()
    {
        _viewer = null;
        Log.Info("[AccessibilityMod] MapScreen popped");
    }

    public void UpdateStartPoint(MapPoint point)
    {
        _startPoint = point;
        if (_viewer != null)
        {
            var node = _handler.GetNode(point);
            if (node != null)
                _viewer.SetStartNode(node);
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (_viewer == null) return false;

        string? result = action.Key switch
        {
            "buffer_next_item" => _viewer.MoveForward(),   // Ctrl+Up
            "buffer_prev_item" => _viewer.MoveBackward(),  // Ctrl+Down
            "buffer_next" => _viewer.NextBranch(),          // Ctrl+Right
            "buffer_prev" => _viewer.PreviousBranch(),      // Ctrl+Left
            _ => null,
        };

        if (result != null)
        {
            SpeechManager.Output(result);
            return true;
        }

        return false;
    }
}
