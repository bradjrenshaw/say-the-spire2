using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CompendiumMenuScreen : GameScreen
{
    private readonly NCompendiumSubmenu _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = "Compendium",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly System.Collections.Generic.List<NClickableControl> _buttons = new();

    public override string? ScreenName => "Compendium";

    public CompendiumMenuScreen(NCompendiumSubmenu screen)
    {
        _screen = screen;
        RootElement = _root;
    }

    protected override void BuildRegistry()
    {
        _root.Clear();
        _buttons.Clear();

        RegisterButton("%CardLibraryButton");
        RegisterButton("%RelicCollectionButton");
        RegisterButton("%PotionLabButton");
        RegisterButton("%StatisticsButton");
        RegisterButton("%RunHistoryButton");
        RegisterButton("%ConfirmButton");
        WireFocusNeighbors();
    }

    private void RegisterButton(string nodePath)
    {
        var control = _screen.GetNodeOrNull<NClickableControl>(nodePath);
        if (control == null || !control.Visible)
            return;

        var proxy = ProxyFactory.Create(control);
        _root.Add(proxy);
        _buttons.Add(control);
        Register(control, proxy);
    }

    private void WireFocusNeighbors()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var self = _buttons[i].GetPath();
            _buttons[i].FocusNeighborTop = i > 0 ? _buttons[i - 1].GetPath() : self;
            _buttons[i].FocusNeighborBottom = i < _buttons.Count - 1 ? _buttons[i + 1].GetPath() : self;
            _buttons[i].FocusNeighborLeft = self;
            _buttons[i].FocusNeighborRight = self;
        }
    }
}
