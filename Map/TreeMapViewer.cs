using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Map;

public class TreeMapViewer : MapViewer
{
    private readonly Stack<MapEdge> _pathStack = new();
    private readonly Stack<MapReachabilityContext> _contextStack = new();
    private MapReachabilityContext _currentContext;

    // Cached row nodes for left/right navigation
    private List<MapNode> _rowNodes = new();
    private int _rowIndex;

    private static bool AutoAdvance => ModSettings.GetValue<bool>("map.auto_advance");
    private static bool AutoAdvanceBackwards => ModSettings.GetValue<bool>("map.auto_advance_backward");
    private static bool VerboseBackward => ModSettings.GetValue<bool>("map.verbose_backward");

    public TreeMapViewer(MapHandler handler) : base(handler)
    {
    }

    public override void SetStartNode(MapNode focusedNode)
    {
        _pathStack.Clear();
        _contextStack.Clear();
        Current = focusedNode;
        _currentContext = ResolveStartContext(focusedNode);
        RefreshSiblings();
    }

    public override string? JumpToNode(MapNode node)
    {
        SetStartNode(node);
        return MapNodeAnnouncementFormatter.DescribeNode(node, Handler, _rowNodes,
            includeChoicePrefix: true, travelOrigin: GetChoiceOrigin(),
            travelContext: GetChoiceContext(), nodeContext: _currentContext);
    }

    public override string? MoveForward()
    {
        if (Current == null) return null;

        var forwardNodes = GetForwardNodes(Current, _currentContext);
        if (forwardNodes.Count == 0)
            return LocalizationManager.Get("map_nav", "NAV.NO_FORWARD");

        // Always move to leftmost child by column
        var nextNode = forwardNodes[0];
        if (!MapReachability.TryAdvance(Current, nextNode, Handler, _currentContext, out var nextContext))
            return LocalizationManager.Get("map_nav", "NAV.NO_FORWARD");

        var edge = Current.ForwardEdges.FirstOrDefault(e => e.To == nextNode) ?? new MapEdge(Current, nextNode);
        _pathStack.Push(edge);
        _contextStack.Push(_currentContext);
        Current = nextNode;
        _currentContext = nextContext;
        RefreshSiblings();

        if (AutoAdvance)
            return AutoAdvanceForward();

        var announcement = AnnounceCurrentNode();
        if (_rowNodes.Count > 1)
            announcement = GetChoiceText() + ", " + announcement;
        return announcement;
    }

    public override string? MoveBackward()
    {
        if (Current == null) return null;

        if (_pathStack.Count > 0)
        {
            var edge = _pathStack.Pop();
            _currentContext = _contextStack.Pop();
            Current = edge.From;
            RefreshSiblings();

            if (AutoAdvanceBackwards)
                return AutoAdvanceBackward();

            return AnnounceBackwardNode();
        }

        // No path stack — try to go to a parent
        var parents = Current.BackwardEdges;
        if (parents.Count == 0)
            return LocalizationManager.Get("map_nav", "NAV.NO_BACKWARD");

        // Prefer traveled parent
        var parent = parents.FirstOrDefault(e => e.From.State == MapPointState.Traveled)
                     ?? parents[0];

        Current = parent.From;
        _currentContext = ResolveStartContext(Current);
        RefreshSiblings();

        if (AutoAdvanceBackwards)
            return AutoAdvanceBackward();

        return AnnounceBackwardNode();
    }

    public override string? NextBranch()
    {
        if (Current == null) return null;
        if (_rowNodes.Count <= 1 || _rowIndex >= _rowNodes.Count - 1)
            return null;

        var origin = GetChoiceOrigin();
        var choiceContext = GetChoiceContext();
        _rowIndex++;
        var nextNode = _rowNodes[_rowIndex];
        var nextContext = ResolveStartContext(nextNode);
        if (origin != null && !MapReachability.TryAdvance(origin, nextNode, Handler, choiceContext, out nextContext))
            return null;

        Current = nextNode;
        _currentContext = nextContext;
        return AnnounceCurrentNode();
    }

    public override string? PreviousBranch()
    {
        if (Current == null) return null;
        if (_rowNodes.Count <= 1 || _rowIndex <= 0)
            return null;

        var origin = GetChoiceOrigin();
        var choiceContext = GetChoiceContext();
        _rowIndex--;
        var nextNode = _rowNodes[_rowIndex];
        var nextContext = ResolveStartContext(nextNode);
        if (origin != null && !MapReachability.TryAdvance(origin, nextNode, Handler, choiceContext, out nextContext))
            return null;

        Current = nextNode;
        _currentContext = nextContext;
        return AnnounceCurrentNode();
    }

    private void RefreshSiblings()
    {
        if (Current == null) return;

        // Determine which parent we came from
        MapNode? parent = null;
        if (_pathStack.Count > 0)
        {
            // We got here via a forward move — the parent is the edge's From
            parent = _pathStack.Peek().From;
        }
        else
        {
            // Initial focus or after exhausting the stack — pick a parent
            var parentEdge = Current.BackwardEdges
                .FirstOrDefault(e => e.From.State == MapPointState.Traveled)
                ?? Current.BackwardEdges.FirstOrDefault();
            parent = parentEdge?.From;
        }

        if (parent != null)
        {
            _rowNodes = GetForwardNodes(parent, GetChoiceContext());
        }
        else
        {
            // No parent (e.g., ancient node) — just this node
            _rowNodes = new List<MapNode> { Current };
        }

        _rowIndex = _rowNodes.IndexOf(Current);
        if (_rowIndex < 0) _rowIndex = 0;
    }

    public string AnnounceCurrentNode()
    {
        return MapNodeAnnouncementFormatter.DescribeNode(Current!, Handler, _rowNodes,
            travelOrigin: GetChoiceOrigin(), travelContext: GetChoiceContext(), nodeContext: _currentContext);
    }

    private string AnnounceBackwardNode()
    {
        var announcement = AnnounceCurrentNode();
        if (_rowNodes.Count > 1)
            announcement = GetChoiceText() + ", " + announcement;
        return announcement;
    }

    private string AutoAdvanceForward()
    {
        var sb = new StringBuilder();

        // Current is already set by MoveForward, RefreshSiblings already called.
        // Algorithm: if node has siblings (parent had >1 child) it's a choice — stop.
        // Otherwise add to path and advance.
        while (true)
        {
            if (_rowNodes.Count > 1)
            {
                // Choice row — announce choice + this node, then stop
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(GetChoiceText());
                sb.Append(", ");
                sb.Append(MapNodeAnnouncementFormatter.DescribeNode(Current!, Handler, _rowNodes,
                    travelOrigin: GetChoiceOrigin(), travelContext: GetChoiceContext(), nodeContext: _currentContext));
                break;
            }

            // Not a choice — add to path
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(Current!.GetDisplayName());

            // Try to advance
            var forwardNodes = GetForwardNodes(Current!, _currentContext);
            if (forwardNodes.Count == 0)
                break;

            var nextNode = forwardNodes[0];
            if (!MapReachability.TryAdvance(Current, nextNode, Handler, _currentContext, out var nextContext))
                break;

            var edge = Current.ForwardEdges.FirstOrDefault(e => e.To == nextNode) ?? new MapEdge(Current, nextNode);
            _pathStack.Push(edge);
            _contextStack.Push(_currentContext);
            Current = nextNode;
            _currentContext = nextContext;
            RefreshSiblings();
        }

        return sb.ToString();
    }

    private string AutoAdvanceBackward()
    {
        var sb = new StringBuilder();
        var visited = new List<MapNode> { Current! };

        while (true)
        {
            RefreshSiblings();

            // Stop if we've reached a choice node (parent had multiple children)
            if (_rowNodes.Count > 1)
                break;

            // Try path stack first
            if (_pathStack.Count > 0)
            {
                var edge = _pathStack.Pop();
                var previousContext = _contextStack.Pop();
                if (edge.From != Current!.BackwardEdges.FirstOrDefault()?.From)
                {
                    _pathStack.Push(edge);
                    _contextStack.Push(previousContext);
                    break;
                }
                _currentContext = previousContext;
                Current = edge.From;
                visited.Add(Current);
            }
            else
            {
                // No stack — navigate via backward edges
                var parents = Current!.BackwardEdges;
                if (parents.Count == 0)
                    break;

                var parent = parents.FirstOrDefault(e => e.From.State == MapPointState.Traveled)
                             ?? parents[0];
                Current = parent.From;
                _currentContext = ResolveStartContext(Current);
                visited.Add(Current);
            }
        }

        RefreshSiblings();

        // All intermediate nodes get short names (if verbose)
        if (VerboseBackward)
        {
            for (int i = 0; i < visited.Count - 1; i++)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(visited[i].GetDisplayName());
            }
        }

        // Final node gets full announcement; prefix with "choice" if it's a choice node
        if (sb.Length > 0) sb.Append(", ");
        if (_rowNodes.Count > 1)
        {
            sb.Append(GetChoiceText());
            sb.Append(", ");
        }
        sb.Append(MapNodeAnnouncementFormatter.DescribeNode(visited[^1], Handler, _rowNodes,
            travelOrigin: GetChoiceOrigin(), travelContext: GetChoiceContext(), nodeContext: _currentContext));

        return sb.ToString();
    }

    private static string GetChoiceText()
    {
        return LocalizationManager.Get("map_nav", "NAV.CHOICE") ?? "choice";
    }

    private List<MapNode> GetForwardNodes(MapNode node, MapReachabilityContext context)
    {
        return MapReachability.GetForwardNodes(node, Handler, context);
    }

    private MapNode? GetChoiceOrigin()
    {
        if (Current == null)
            return null;

        if (_pathStack.Count > 0)
            return _pathStack.Peek().From;

        var parentEdge = Current.BackwardEdges
            .FirstOrDefault(e => e.From.State == MapPointState.Traveled)
            ?? Current.BackwardEdges.FirstOrDefault();
        return parentEdge?.From;
    }

    private MapReachabilityContext GetChoiceContext()
    {
        return _contextStack.Count > 0 ? _contextStack.Peek() : Handler.ReachabilityContext;
    }

    private MapReachabilityContext ResolveStartContext(MapNode node)
    {
        var start = Handler.CurrentNode;
        if (start == null)
            return Handler.ReachabilityContext;

        return MapReachability.TryGetBestContextAtNode(start, Handler, Handler.ReachabilityContext, node, out var context)
            ? context
            : Handler.ReachabilityContext;
    }
}
