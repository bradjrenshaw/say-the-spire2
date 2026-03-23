using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class HandSelectGameScreen : GameScreen
{
    public static HandSelectGameScreen? Current { get; private set; }

    private readonly NPlayerHand _hand;
    private readonly string _containerLabel;

    // Stable containers — created once, reused across rebuilds
    private readonly ListContainer _root = new();
    private readonly ListContainer _handList = new()
    {
        ContainerLabel = "Hand",
        AnnouncePosition = true,
    };
    private readonly ListContainer _selectedList = new()
    {
        ContainerLabel = "Selected",
        AnnouncePosition = true,
    };

    // Stable proxy cache — keyed by card holder identity
    private readonly Dictionary<NCardHolder, ProxyCard> _proxyCache = new();
    // Track which selected holders we've connected focus signals to
    private readonly HashSet<NCardHolder> _connectedSelectedHolders = new();

    public override string? ScreenName => _containerLabel;

    public HandSelectGameScreen(NPlayerHand hand, string label)
    {
        _hand = hand;
        _containerLabel = label;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        ClearRegistry();
        _handList.Clear();
        _selectedList.Clear();
        _root.Clear();

        var handHolders = new List<Control>();
        foreach (var holder in _hand.ActiveHolders)
        {
            if (holder == null) continue;
            var proxy = GetOrCreateProxy(holder);
            _handList.Add(proxy);
            Register(holder, proxy);
            handHolders.Add(holder);
        }

        var selectedHolders = new List<Control>();
        var selectedContainer = _hand.GetNodeOrNull<NSelectedHandCardContainer>("%SelectedHandCardContainer");
        if (selectedContainer != null)
        {
            foreach (var holder in selectedContainer.Holders)
            {
                holder.FocusMode = Control.FocusModeEnum.All;
                var proxy = GetOrCreateProxy(holder);
                _selectedList.Add(proxy);
                Register(holder, proxy);
                selectedHolders.Add(holder);

                // NSelectedHandCardHolder doesn't extend NClickableControl,
                // so RefreshFocus won't fire. Connect to FocusEntered signal instead.
                if (!_connectedSelectedHolders.Contains(holder))
                {
                    _connectedSelectedHolders.Add(holder);
                    holder.FocusEntered += () => UI.UIManager.SetFocusedControl(holder, proxy);
                }
            }
        }

        // Focus navigation: hand left/right, down to selected; selected left/right, up to hand
        for (int i = 0; i < handHolders.Count; i++)
        {
            var self = handHolders[i].GetPath();
            handHolders[i].FocusNeighborLeft = i > 0 ? handHolders[i - 1].GetPath() : handHolders[^1].GetPath();
            handHolders[i].FocusNeighborRight = i < handHolders.Count - 1 ? handHolders[i + 1].GetPath() : handHolders[0].GetPath();
            handHolders[i].FocusNeighborTop = self;
            handHolders[i].FocusNeighborBottom = selectedHolders.Count > 0 ? selectedHolders[0].GetPath() : self;
        }

        for (int i = 0; i < selectedHolders.Count; i++)
        {
            var self = selectedHolders[i].GetPath();
            selectedHolders[i].FocusNeighborLeft = i > 0 ? selectedHolders[i - 1].GetPath() : selectedHolders[^1].GetPath();
            selectedHolders[i].FocusNeighborRight = i < selectedHolders.Count - 1 ? selectedHolders[i + 1].GetPath() : selectedHolders[0].GetPath();
            selectedHolders[i].FocusNeighborTop = handHolders.Count > 0 ? handHolders[0].GetPath() : self;
            selectedHolders[i].FocusNeighborBottom = self;
        }

        _root.Add(_handList);
        if (selectedHolders.Count > 0)
            _root.Add(_selectedList);
        RootElement = _root;
    }

    protected override void BuildRegistry()
    {
        // Initial build handled by first OnUpdate call
        RootElement = _root;
    }

    private ProxyCard GetOrCreateProxy(NCardHolder holder)
    {
        if (!_proxyCache.TryGetValue(holder, out var proxy))
        {
            proxy = new ProxyCard(holder);
            _proxyCache[holder] = proxy;
        }
        return proxy;
    }
}
