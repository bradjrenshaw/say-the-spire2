using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves.Runs;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RunHistoryGameScreen : GameScreen
{
    private static readonly MethodInfo? SelectPlayerMethod =
        AccessTools.Method(typeof(NRunHistory), "SelectPlayer");
    private bool _preferNavigationFocus = true;

    private readonly NRunHistory _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "RUN_HISTORY.SCREEN_NAME"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyRunHistoryPlayerIcon> _playerProxyCache = new();
    private readonly Dictionary<Control, ProxyRunHistoryMapPoint> _mapProxyCache = new();
    private List<SerializableBadge>? _selectedBadges;
    private string? _stateToken;

    public override Message? ScreenName => Ui("RUN_HISTORY.SCREEN_NAME");

    public RunHistoryGameScreen(NRunHistory screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_select");
        ClaimAction("ui_accept");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
    }

    public override void OnPush()
    {
        base.OnPush();
        _stateToken = BuildStateToken();
        EnsureFocus();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _playerProxyCache.Clear();
        _mapProxyCache.Clear();
        _selectedBadges = null;
        _stateToken = null;
    }

    public override void OnUpdate()
    {
        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }

        EnsureFocus();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "mega_view_deck_and_tab_left")
            return ChangeRun(-1);

        if (action.Key == "mega_view_exhaust_pile_and_tab_right")
            return ChangeRun(1);

        if (action.Key is not "ui_select" and not "ui_accept")
            return false;

        var focused = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focused is NRunHistoryPlayerIcon icon)
        {
            SelectPlayerMethod?.Invoke(_screen, new object[] { icon });
            _selectedBadges = icon.Player.Badges.ToList();
            return true;
        }

        if (focused is NClickableControl button)
        {
            button.EmitSignal(NClickableControl.SignalName.Released, button);
            return true;
        }

        return false;
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        var navigation = NewRow(Ui("RUN_HISTORY.ROWS.RUNS"));
        var players = NewRow(Ui("RUN_HISTORY.ROWS.PLAYERS"));
        var summary = NewRow(Ui("RUN_HISTORY.ROWS.SUMMARY"));
        var details = NewRow(Ui("RUN_HISTORY.ROWS.DETAILS"));
        var badges = NewRow(Ui("RUN_HISTORY.ROWS.BADGES"));
        var map = NewRow(Ui("RUN_HISTORY.ROWS.MAP"));
        var potions = NewRow(Ui("RUN_HISTORY.ROWS.POTIONS"));
        var relics = NewRow(Ui("RUN_HISTORY.ROWS.RELICS"));
        var deck = NewRow(Ui("RUN_HISTORY.ROWS.DECK"));
        var quote = NewRow(Ui("RUN_HISTORY.ROWS.OUTCOME"));

        RegisterNavigation(navigation);
        RegisterPlayers(players);
        RegisterSummary(summary);
        RegisterDetails(details);
        RegisterBadges(badges);
        RegisterMapHistory(map);
        RegisterPotions(potions);
        RegisterRelics(relics);
        RegisterDeck(deck);
        RegisterQuote(quote);

        AddIfNotEmpty(navigation);
        AddIfNotEmpty(players);
        AddIfNotEmpty(summary);
        AddIfNotEmpty(details);
        AddIfNotEmpty(badges);
        AddIfNotEmpty(map);
        AddIfNotEmpty(potions);
        AddIfNotEmpty(relics);
        AddIfNotEmpty(deck);
        AddIfNotEmpty(quote);

        if (Settings.UIEnhancementsSettings.RunHistory.Get())
            WireFocusNeighbors();
    }

    private static ListContainer NewRow(Message label) => new()
    {
        ContainerLabel = label,
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private void AddIfNotEmpty(ListContainer row)
    {
        if (row.Children.Count > 0)
            _root.Add(row);
    }

    private void RegisterNavigation(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<NClickableControl>("LeftArrow"), Ui("RUN_HISTORY.PREVIOUS_RUN"));
        RegisterStatic(container, _screen.GetNodeOrNull<NClickableControl>("RightArrow"), Ui("RUN_HISTORY.NEXT_RUN"));
    }

    private void RegisterPlayers(ListContainer container)
    {
        var playerContainer = _screen.GetNodeOrNull<Control>("%PlayerIconContainer");
        if (playerContainer == null)
            return;

        foreach (var icon in playerContainer.GetChildren().OfType<NRunHistoryPlayerIcon>())
        {
            var proxy = GetOrCreatePlayerProxy(icon);
            container.Add(proxy);
            Register(icon, proxy);
        }

        _selectedBadges ??= InferSelectedBadges();
    }

    private void RegisterSummary(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%HpLabel"), Ui("RUN_HISTORY.FIELDS.HP"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%GoldLabel"), Ui("RUN_HISTORY.FIELDS.GOLD"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%FloorNumLabel"), Ui("RUN_HISTORY.FIELDS.FLOOR"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%RunTimeLabel"), Ui("RUN_HISTORY.FIELDS.RUN_TIME"));
    }

    private void RegisterDetails(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%DateLabel"), Ui("RUN_HISTORY.FIELDS.DATE"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%SeedLabel"), Ui("RUN_HISTORY.FIELDS.SEED"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%GameModeLabel"), Ui("RUN_HISTORY.FIELDS.MODE"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%BuildLabel"), Ui("RUN_HISTORY.FIELDS.BUILD"));
    }

    private void RegisterBadges(ListContainer container)
    {
        var badgeControls = GetBadgeControls();
        if (badgeControls.Count == 0)
            return;

        var allBadges = GetAllPlayerBadges();
        for (var i = 0; i < badgeControls.Count; i++)
        {
            var badge = FindBadgeData(badgeControls[i], allBadges)
                ?? _selectedBadges?.ElementAtOrDefault(i);
            var proxy = new ProxyBadge(badgeControls[i], badge);
            container.Add(proxy);
            Register(badgeControls[i], proxy);
        }
    }

    private void RegisterMapHistory(ListContainer container)
    {
        var actsContainer = _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts");
        if (actsContainer == null)
            return;

        foreach (var point in actsContainer.GetChildren()
                     .OfType<NActHistoryEntry>()
                     .SelectMany(act => act.Entries))
        {
            var proxy = GetOrCreateMapProxy(point);
            container.Add(proxy);
            Register(point, proxy);
        }
    }

    private void RegisterPotions(ListContainer container)
    {
        var potionContainer = _screen.GetNodeOrNull<Control>("%PotionHolders");
        if (potionContainer == null)
            return;

        foreach (var holder in potionContainer.GetChildren().OfType<NPotionHolder>())
        {
            var proxy = new ProxyPotionHolder(holder);
            container.Add(proxy);
            Register(holder, proxy);
        }
    }

    private void RegisterRelics(ListContainer container)
    {
        var relicHistory = _screen.GetNodeOrNull<Control>("%RelicHistory");
        SetContainerLabelFromHeader(container, relicHistory?.GetNodeOrNull<Control>("Header"));

        var relicsContainer = relicHistory?.GetNodeOrNull<Control>("%RelicsContainer");
        if (relicsContainer == null)
            return;

        foreach (var holder in relicsContainer.GetChildren().OfType<NRelicBasicHolder>())
        {
            var proxy = new ProxyRunHistoryRelicHolder(holder);
            container.Add(proxy);
            Register(holder, proxy);
        }
    }

    private void RegisterDeck(ListContainer container)
    {
        var deckHistory = _screen.GetNodeOrNull<Control>("%DeckHistory");
        SetContainerLabelFromHeader(container, deckHistory?.GetNodeOrNull<Control>("Header"));

        var cardContainer = deckHistory?.GetNodeOrNull<Control>("%CardContainer");
        if (cardContainer == null)
            return;

        foreach (var entry in cardContainer.GetChildren().OfType<NDeckHistoryEntry>())
        {
            var proxy = new ProxyDeckHistoryEntry(entry);
            container.Add(proxy);
            Register(entry, proxy);
        }
    }

    private void RegisterQuote(ListContainer container)
    {
        var control = _screen.GetNodeOrNull<Control>("%DeathQuoteLabel");
        if (control == null || !control.Visible)
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        var element = new ActionElement(
            () => null,
            status: () => GetStaticStatus(Ui("RUN_HISTORY.ROWS.OUTCOME"), control));
        ConnectFocusSignal(control, element);
        container.Add(element);
        Register(control, element);
    }

    private void RegisterStatic(ListContainer container, Control? control, Message label)
    {
        if (control == null || !control.Visible)
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        var element = new ActionElement(
            () => label,
            status: () => GetStaticStatus(label, control));
        ConnectFocusSignal(control, element);
        container.Add(element);
        Register(control, element);
    }

    private ProxyRunHistoryPlayerIcon GetOrCreatePlayerProxy(NRunHistoryPlayerIcon icon)
    {
        if (_playerProxyCache.TryGetValue(icon, out var proxy))
            return proxy;

        proxy = new ProxyRunHistoryPlayerIcon(icon);
        _playerProxyCache[icon] = proxy;
        return proxy;
    }

    private ProxyRunHistoryMapPoint GetOrCreateMapProxy(NMapPointHistoryEntry point)
    {
        if (_mapProxyCache.TryGetValue(point, out var proxy))
            return proxy;

        proxy = new ProxyRunHistoryMapPoint(point);
        _mapProxyCache[point] = proxy;
        return proxy;
    }

    private string BuildStateToken()
    {
        var players = _screen.GetNodeOrNull<Control>("%PlayerIconContainer")?.GetChildCount() ?? 0;
        var acts = _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts")?.GetChildCount() ?? 0;
        var potions = _screen.GetNodeOrNull<Control>("%PotionHolders")?.GetChildCount() ?? 0;
        var relics = _screen.GetNodeOrNull<Control>("%RelicHistory")?.GetNodeOrNull<Control>("%RelicsContainer")?.GetChildCount() ?? 0;
        var cards = _screen.GetNodeOrNull<Control>("%DeckHistory")?.GetNodeOrNull<Control>("%CardContainer")?.GetChildCount() ?? 0;
        return string.Join("|",
            players, acts, potions, relics, cards,
            GetBadgeToken(),
            GetStaticText(_screen.GetNodeOrNull<Control>("%HpLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%GoldLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%FloorNumLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%RunTimeLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%DateLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%SeedLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%RelicHistory")?.GetNodeOrNull<Control>("Header")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%DeckHistory")?.GetNodeOrNull<Control>("Header")) ?? "");
    }

    private void WireFocusNeighbors()
    {
        var rows = new List<List<Control>>
        {
            GetControls("LeftArrow", "RightArrow"),
            GetContainerControls("%PlayerIconContainer"),
            GetControls("%HpLabel", "%GoldLabel", "%FloorNumLabel", "%RunTimeLabel"),
            GetControls("%DateLabel", "%SeedLabel", "%GameModeLabel", "%BuildLabel"),
            GetContainerControls("%BadgeContainer"),
            GetMapRows(),
            GetContainerControls("%PotionHolders"),
            GetContainerControls("%RelicHistory", "%RelicsContainer"),
            GetContainerControls("%DeckHistory", "%CardContainer"),
            GetControls("%DeathQuoteLabel"),
        }.Where(row => row.Count > 0).ToList();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (int col = 0; col < row.Count; col++)
            {
                var self = row[col].GetPath();
                row[col].FocusNeighborLeft = col > 0 ? row[col - 1].GetPath() : self;
                row[col].FocusNeighborRight = col < row.Count - 1 ? row[col + 1].GetPath() : self;
                row[col].FocusNeighborTop = rowIndex > 0
                    ? rows[rowIndex - 1][System.Math.Min(col, rows[rowIndex - 1].Count - 1)].GetPath()
                    : self;
                row[col].FocusNeighborBottom = rowIndex < rows.Count - 1
                    ? rows[rowIndex + 1][System.Math.Min(col, rows[rowIndex + 1].Count - 1)].GetPath()
                    : self;
            }
        }
    }

    private List<Control> GetControls(params string[] paths)
    {
        return paths.Select(path => _screen.GetNodeOrNull<Control>(path))
            .Where(control => control != null && control.Visible)
            .Cast<Control>()
            .ToList();
    }

    private List<Control> GetContainerControls(string containerPath)
    {
        return _screen.GetNodeOrNull<Control>(containerPath)?.GetChildren().OfType<Control>().Where(control => control.Visible).ToList()
            ?? new List<Control>();
    }

    private List<NBadge> GetBadgeControls()
    {
        return _screen.GetNodeOrNull<Control>("%BadgeContainer")
            ?.GetChildren().OfType<NBadge>().Where(control => control.Visible).ToList()
            ?? new List<NBadge>();
    }

    private List<Control> GetContainerControls(string parentPath, string childPath)
    {
        return _screen.GetNodeOrNull<Control>(parentPath)?.GetNodeOrNull<Control>(childPath)?.GetChildren().OfType<Control>().Where(control => control.Visible).ToList()
            ?? new List<Control>();
    }

    private static void SetContainerLabelFromHeader(ListContainer container, Control? header)
    {
        var text = GetStaticText(header);
        if (!string.IsNullOrWhiteSpace(text))
            container.ContainerLabel = Message.Raw(text);
    }

    private List<Control> GetMapRows()
    {
        return _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts")
            ?.GetChildren().OfType<NActHistoryEntry>().SelectMany(act => act.Entries).Cast<Control>().ToList()
            ?? new List<Control>();
    }

    private static string? GetStaticText(Control? control)
    {
        return control switch
        {
            RichTextLabel richText => ProxyElement.StripBbcode(richText.Text).Trim(),
            Label label => label.Text.Trim(),
            null => null,
            _ => ProxyElement.FindChildTextPublic(control)?.Trim(),
        };
    }

    private static Message? GetStaticStatus(Message label, Control? control)
    {
        var text = GetStaticText(control);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var labelText = label.Resolve();
        var prefixedColon = $"{labelText}:";
        if (text.StartsWith(prefixedColon))
            return Message.Raw(text[prefixedColon.Length..].Trim());

        var prefixedComma = $"{labelText},";
        if (text.StartsWith(prefixedComma))
            return Message.Raw(text[prefixedComma.Length..].Trim());

        if (text == labelText)
            return null;

        return Message.Raw(text);
    }

    private bool ChangeRun(int direction)
    {
        var control = direction < 0
            ? _screen.GetNodeOrNull<NClickableControl>("LeftArrow")
            : _screen.GetNodeOrNull<NClickableControl>("RightArrow");

        if (control == null || !control.Visible || !control.IsEnabled)
            return true;

        _preferNavigationFocus = true;
        _selectedBadges = null;
        control.EmitSignal(NClickableControl.SignalName.Released, control);
        return true;
    }

    private List<SerializableBadge>? InferSelectedBadges()
    {
        var icons = _screen.GetNodeOrNull<Control>("%PlayerIconContainer")
            ?.GetChildren().OfType<NRunHistoryPlayerIcon>().ToList();
        if (icons == null || icons.Count == 0)
            return null;

        if (icons.Count == 1)
            return icons[0].Player.Badges.ToList();

        var badgeCount = GetBadgeControls().Count;
        var matches = icons.Where(icon => icon.Player.Badges.Count() == badgeCount).ToList();
        return matches.Count == 1 ? matches[0].Player.Badges.ToList() : null;
    }

    private List<SerializableBadge> GetAllPlayerBadges()
    {
        return _screen.GetNodeOrNull<Control>("%PlayerIconContainer")
            ?.GetChildren().OfType<NRunHistoryPlayerIcon>()
            .SelectMany(icon => icon.Player.Badges)
            .ToList()
            ?? new List<SerializableBadge>();
    }

    private static SerializableBadge? FindBadgeData(NBadge control, IReadOnlyList<SerializableBadge> badges)
    {
        if (badges.Count == 0)
            return null;

        var iconPath = NormalizeResourcePath(control.GetNodeOrNull<TextureRect>("%Icon")?.Texture);
        var holderPath = NormalizeResourcePath(control.GetNodeOrNull<TextureRect>("%BadgeHolder")?.Texture);

        var exact = badges.FirstOrDefault(badge =>
            MatchesBadgeIcon(iconPath, badge)
            && MatchesBadgeRarity(holderPath, badge));
        if (exact != null)
            return exact;

        return badges.FirstOrDefault(badge => MatchesBadgeIcon(iconPath, badge));
    }

    private string GetBadgeToken()
    {
        var visibleBadges = string.Join(",",
            GetBadgeControls().Select(control =>
                NormalizeResourcePath(control.GetNodeOrNull<TextureRect>("%Icon")?.Texture) ?? control.Name.ToString()));
        var selectedBadges = _selectedBadges == null
            ? ""
            : string.Join(",", _selectedBadges.Select(badge => $"{badge.Id}:{badge.Rarity}"));
        return $"{visibleBadges}|{selectedBadges}";
    }

    private static bool MatchesBadgeIcon(string? iconPath, SerializableBadge badge)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || string.IsNullOrWhiteSpace(badge.Id))
            return false;

        return iconPath.Contains($"/badge_{badge.Id.ToLowerInvariant()}.png");
    }

    private static bool MatchesBadgeRarity(string? holderPath, SerializableBadge badge)
    {
        if (string.IsNullOrWhiteSpace(holderPath))
            return false;

        return holderPath.Contains($"/badge_{badge.Rarity.ToString().ToLowerInvariant()}.png");
    }

    private static string? NormalizeResourcePath(Resource? resource)
    {
        var path = resource?.ResourcePath;
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Replace('\\', '/').ToLowerInvariant();
    }

    private void EnsureFocus()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (!_preferNavigationFocus && focusOwner != null && _screen.IsAncestorOf(focusOwner))
            return;

        if (FocusPreferredNavigation())
            _preferNavigationFocus = false;
    }

    private bool FocusPreferredNavigation()
    {
        var previous = _screen.GetNodeOrNull<NClickableControl>("LeftArrow");
        if (TryFocus(previous))
            return true;

        var next = _screen.GetNodeOrNull<NClickableControl>("RightArrow");
        return TryFocus(next);
    }

    private static bool TryFocus(Control? control)
    {
        if (control == null || !control.Visible || control.FocusMode == Control.FocusModeEnum.None)
            return false;

        control.GrabFocus();
        return true;
    }

    private static Message Ui(string key) => Message.Localized("ui", key);
}
