using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RestSiteGameScreen : GameScreen
{
    public static RestSiteGameScreen? Current { get; private set; }

    private readonly NRestSiteRoom _room;
    private string? _stateToken;
    private bool _noActionsRemain;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.REST_SITE");

    public RestSiteGameScreen(NRestSiteRoom room)
    {
        _room = room;
    }

    public override void OnPush()
    {
        Current = this;
        base.OnPush();
        _stateToken = BuildStateToken();
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public override bool ShouldSuppressFocusAnnouncement(Control control) =>
        _noActionsRemain && control is NRestSiteButton;

    public override void OnUpdate()
    {
        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }
    }

    private string? BuildStateToken()
    {
        var container = _room.GetNodeOrNull<Godot.Control>("%ChoicesContainer");
        if (container == null) return null;
        var buttons = container.GetChildren().OfType<NRestSiteButton>().Where(b => b.Visible);
        // Fold each button's clickable state into the token. Selecting an
        // option calls NRestSiteRoom.DisableOptions() (button.Disable()),
        // flipping IsEnabled synchronously; Miniature Tent recreates the
        // buttons enabled afterward. Tracking IsEnabled (not the static
        // RestSiteOption.IsEnabled, which never changes) is what lets the
        // disable/re-enable transition trigger a rebuild.
        return string.Join("|", buttons.Select(b => $"{b.Name}:{b.IsEnabled}"));
    }

    protected override void BuildRegistry()
    {
        var container = _room.GetNodeOrNull<Control>("%ChoicesContainer");
        if (container == null) return;

        var buttons = container.GetChildren().OfType<NRestSiteButton>().Where(b => b.Visible).ToList();
        if (buttons.Count == 0)
        {
            _noActionsRemain = true;
            return;
        }

        // When no rest action is still available (the buttons remain visible
        // but disabled after the final action), leave the screen with no
        // labeled root. Otherwise the game refocusing the rest site — e.g.
        // after the upgrade card-grid closes — re-announces the rest prompt
        // even though there's nothing left to do. Miniature Tent (and other
        // extra-action sources) re-enable the buttons via UpdateRestSiteOptions
        // after the action, so the screen still builds and announces then.
        // Use the button's clickable state, NOT RestSiteOption.IsEnabled —
        // the latter is static eligibility and never flips post-action.
        if (!buttons.Any(b => b.IsEnabled))
        {
            RootElement = null;
            // Also veto focus announcements: the game auto-focuses a now-
            // disabled option button after the final action, which would
            // otherwise be read out via the generic focus path even though
            // RootElement is gone.
            _noActionsRemain = true;
            return;
        }
        _noActionsRemain = false;

        var headerText = new LocString("rest_site_ui", "PROMPT").GetFormattedText();

        var list = new ListContainer
        {
            ContainerLabel = Message.Raw(headerText),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = list;

        foreach (var button in buttons)
        {
            var proxy = new ProxyRestSiteButton(button);
            list.Add(proxy);
            Register(button, proxy);
        }

        // Constrain focus so it can't escape the rest site buttons
        if (Settings.UIEnhancementsSettings.RestSite.Get())
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var self = buttons[i].GetPath();
                buttons[i].FocusNeighborTop = self;
                buttons[i].FocusNeighborBottom = self;
                buttons[i].FocusNeighborLeft = i > 0 ? buttons[i - 1].GetPath() : self;
                buttons[i].FocusNeighborRight = i < buttons.Count - 1 ? buttons[i + 1].GetPath() : self;
            }
        }
    }
}
