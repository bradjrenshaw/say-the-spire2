using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using SayTheSpire2.Input;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class StatsGameScreen : GameScreen
{
    private readonly NStatsScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = "Statistics",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyStatEntry> _proxyCache = new();
    private readonly Dictionary<Control, StatsValueElement> _elementCache = new();
    private string? _stateToken;

    public override string? ScreenName => "Statistics";

    public StatsGameScreen(NStatsScreen screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_left");
        ClaimAction("ui_right");
    }

    public override void OnPush()
    {
        base.OnPush();
        _stateToken = BuildStateToken();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _proxyCache.Clear();
        _elementCache.Clear();
        _stateToken = null;
    }

    public override void OnUpdate()
    {
        var token = BuildStateToken();
        if (token == _stateToken)
            return;

        _stateToken = token;
        ClearRegistry();
        BuildRegistry();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        return action.Key switch
        {
            "ui_right" => MoveFocusedValue(1),
            "ui_left" => MoveFocusedValue(-1),
            _ => false,
        };
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        var grid = _screen.GetNodeOrNull<NGeneralStatsGrid>("%StatsGrid");
        if (grid == null)
            return;

        RegisterOverallStats(grid);
        RegisterCharacterStats(grid);
        WireFocusNeighbors();
    }

    private void RegisterOverallStats(NGeneralStatsGrid grid)
    {
        var entries = grid.GetNodeOrNull<Node>("%GridContainer")?.GetChildren().OfType<NStatEntry>().ToList() ?? new List<NStatEntry>();
        var labels = new[]
        {
            "Achievements and epochs",
            "Playtime",
            "Cards",
            "Wins and losses",
            "Monsters",
            "Relics",
            "Potions",
            "Events",
            "Win streak",
        };

        for (int i = 0; i < entries.Count && i < labels.Length; i++)
            AddStat(entries[i], labels[i]);
    }

    private void RegisterCharacterStats(NGeneralStatsGrid grid)
    {
        var characterContainer = grid.GetNodeOrNull<Control>("%CharacterStatsContainer");
        if (characterContainer == null)
            return;

        foreach (var section in characterContainer.GetChildren().OfType<Node>())
        {
            var nameNode = section.GetNodeOrNull<Node>("%NameLabel") ?? section.GetNodeOrNull<Node>("NameLabel");
            var sectionName = nameNode == null ? null : ProxyElement.FindChildTextPublic(nameNode);
            if (string.IsNullOrWhiteSpace(sectionName))
                sectionName = ProxyElement.FindChildTextPublic(section) ?? "Character";

            var statContainer = section.GetNodeOrNull<Node>("%StatsContainer");
            var entries = statContainer?.GetChildren().OfType<NStatEntry>().ToList() ?? new List<NStatEntry>();
            var labels = new[] { "Playtime", "Ascension and wins/losses", "Win streak" };

            for (int i = 0; i < entries.Count && i < labels.Length; i++)
                AddStat(entries[i], $"{sectionName}, {labels[i]}");
        }
    }

    private void AddStat(NStatEntry entry, string label)
    {
        var proxy = GetOrCreateProxy(entry);
        proxy.OverrideLabel = label;
        var element = GetOrCreateElement(entry, proxy);
        _root.Add(element);
        Register(entry, element);
    }

    private StatsValueElement GetOrCreateElement(NStatEntry entry, ProxyStatEntry proxy)
    {
        if (_elementCache.TryGetValue(entry, out var element))
            return element;

        element = new StatsValueElement(
            () => proxy.GetLabel(),
            () => proxy.GetValues());
        _elementCache[entry] = element;
        return element;
    }

    private ProxyStatEntry GetOrCreateProxy(NStatEntry entry)
    {
        if (_proxyCache.TryGetValue(entry, out var proxy))
            return proxy;

        proxy = new ProxyStatEntry(entry);
        _proxyCache[entry] = proxy;
        return proxy;
    }

    private string BuildStateToken()
    {
        var grid = _screen.GetNodeOrNull<NGeneralStatsGrid>("%StatsGrid");
        if (grid == null)
            return "missing";

        var overallCount = grid.GetNodeOrNull<Node>("%GridContainer")?.GetChildCount() ?? 0;
        var characterCount = grid.GetNodeOrNull<Control>("%CharacterStatsContainer")?.GetChildCount() ?? 0;
        var firstCharacter = grid.GetNodeOrNull<Control>("%CharacterStatsContainer")?.GetChildren().OfType<Node>().FirstOrDefault();
        var firstName = firstCharacter == null ? "" : ProxyElement.FindChildTextPublic(firstCharacter) ?? "";
        return $"{overallCount}|{characterCount}|{firstName}";
    }

    private bool MoveFocusedValue(int delta)
    {
        var focused = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focused == null || !_screen.IsAncestorOf(focused))
            return true;

        if (GetElement(focused) is not StatsValueElement element)
            return true;

        if (element.MoveValue(delta))
            UIManager.SetFocusedControl(focused, element);

        return true;
    }

    private void WireFocusNeighbors()
    {
        var controls = GetRegisteredControls()
            .Select(pair => pair.Key)
            .OfType<NStatEntry>()
            .Where(control => control.Visible)
            .ToList();

        for (int i = 0; i < controls.Count; i++)
        {
            var self = controls[i].GetPath();
            controls[i].FocusNeighborTop = i > 0 ? controls[i - 1].GetPath() : self;
            controls[i].FocusNeighborBottom = i < controls.Count - 1 ? controls[i + 1].GetPath() : self;
            controls[i].FocusNeighborLeft = self;
            controls[i].FocusNeighborRight = self;
        }
    }
}
