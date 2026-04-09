using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class RunScreen : Screen
{
    public static RunScreen? Current { get; private set; }

    private static readonly string[] _alwaysEnabled = { "player" };
    public override System.Collections.Generic.IEnumerable<string> AlwaysEnabledBuffers => _alwaysEnabled;

    private Player? _subscribedPlayer;

    // Containers for position announcements out of combat
    private readonly UI.Elements.ListContainer _rootContainer = new() { AnnounceName = false, AnnouncePosition = false };
    private readonly UI.Elements.ListContainer _potionContainer = new() { AnnounceName = false, AnnouncePosition = true };
    private readonly UI.Elements.ListContainer _relicContainer = new() { AnnounceName = false, AnnouncePosition = true };
    private readonly Dictionary<Control, UI.Elements.UIElement> _elementCache = new();

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.TOP_PANEL", "Top Panel"), "mega_top_panel"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.VIEW_DECK", "View Deck"), "mega_view_deck_and_tab_left"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_GOLD", "Announce Gold"), "announce_gold"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_HP", "Announce HP"), "announce_hp"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_BOSS", "Announce Boss"), "announce_boss"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_RELIC_COUNTERS", "Announce Relic Counters"), "announce_relic_counters"),
    };

    public RunScreen()
    {
        ClaimAction("announce_gold");
        ClaimAction("announce_hp");
        ClaimAction("announce_boss");
        ClaimAction("announce_relic_counters");

        _rootContainer.Add(_potionContainer);
        _rootContainer.Add(_relicContainer);
        RootElement = _rootContainer;
    }

    public override void OnPush()
    {
        Current = this;
        SubscribeToPlayer();
    }

    public override void OnPop()
    {
        UnsubscribeFromPlayer();
        Current = null;
    }

    public override void OnUpdate()
    {
        var currentPlayer = GetLocalPlayer();
        if (currentPlayer != null && currentPlayer != _subscribedPlayer)
        {
            UnsubscribeFromPlayer();
            SubscribeToPlayer();
        }

        // Wire focus navigation for relics/potions when not in combat
        // (CombatScreen handles its own wiring during combat)
        if (CombatScreen.Current == null)
            UpdateTopBarNavigation();
    }

    private void SubscribeToPlayer()
    {
        _subscribedPlayer = GetLocalPlayer();
        if (_subscribedPlayer != null)
        {
            _subscribedPlayer.Deck.CardAdded += OnCardObtained;
            _subscribedPlayer.RelicObtained += OnRelicObtained;
            _subscribedPlayer.PotionProcured += OnPotionObtained;
        }
    }

    private void UnsubscribeFromPlayer()
    {
        if (_subscribedPlayer != null)
        {
            _subscribedPlayer.Deck.CardAdded -= OnCardObtained;
            _subscribedPlayer.RelicObtained -= OnRelicObtained;
            _subscribedPlayer.PotionProcured -= OnPotionObtained;
            _subscribedPlayer = null;
        }
    }

    private void OnCardObtained(CardModel card)
    {
        var name = card.Title;
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new CardObtainedEvent(name, _subscribedPlayer?.Creature));
    }

    private void OnRelicObtained(RelicModel relic)
    {
        var name = relic.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new RelicObtainedEvent(name, _subscribedPlayer?.Creature));
    }

    private void OnPotionObtained(PotionModel potion)
    {
        var name = potion.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new PotionObtainedEvent(name, _subscribedPlayer?.Creature));
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "announce_gold":
                AnnounceGold();
                return true;
            case "announce_hp":
                AnnounceHp();
                return true;
            case "announce_boss":
                AnnounceBoss();
                return true;
            case "announce_relic_counters":
                AnnounceRelicCounters();
                return true;
        }

        return false;
    }

    private void AnnounceGold()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output(Message.Localized("ui", "RESOURCE.GOLD", new { amount = player.Gold }));
    }

    private void AnnounceHp()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output(Message.Localized("ui", "RESOURCE.HP", new { current = player.Creature.CurrentHp, max = player.Creature.MaxHp }));
    }

    private void AnnounceBoss()
    {
        if (!RunManager.Instance.IsInProgress) return;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return;

        var boss = runState.Act.BossEncounter;
        var name = boss.Title.GetFormattedText();

        if (runState.Act.HasSecondBoss)
        {
            var second = runState.Act.SecondBossEncounter;
            var secondName = second?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(secondName))
                name = $"{name} and {secondName}";
        }

        SpeechManager.Output(Message.Raw(name));
    }

    private void AnnounceRelicCounters()
    {
        var player = GetLocalPlayer();
        if (player == null) return;

        var parts = new System.Collections.Generic.List<string>();
        foreach (var relic in player.Relics)
        {
            if (relic.ShowCounter && relic.DisplayAmount != 0)
            {
                var name = relic.Title.GetFormattedText();
                if (!string.IsNullOrEmpty(name))
                    parts.Add($"{name}, {relic.DisplayAmount}");
            }
        }

        if (parts.Count == 0)
            SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_RELIC_COUNTERS"));
        else
            SpeechManager.Output(Message.Raw(string.Join(". ", parts)));
    }

    public override UI.Elements.UIElement? GetElement(Control control)
    {
        return _elementCache.TryGetValue(control, out var element) ? element : null;
    }

    private UI.Elements.UIElement GetOrCreateElement(Control control)
    {
        if (_elementCache.TryGetValue(control, out var element))
            return element;
        element = UI.Elements.ProxyFactory.Create(control);
        _elementCache[control] = element;
        return element;
    }

    private void SyncContainer(UI.Elements.ListContainer container, IEnumerable<Control>? controls)
    {
        container.Clear();
        if (controls == null) return;
        foreach (var control in controls)
        {
            if (control != null && GodotObject.IsInstanceValid(control))
                container.Add(GetOrCreateElement(control));
        }
    }

    private void UpdateTopBarNavigation()
    {
        try
        {
            var globalUi = NRun.Instance?.GlobalUi;
            if (globalUi == null) return;

            // Collect potion holders
            var potionHolders = new List<Control>();
            var potionCtrl = globalUi.TopBar?.PotionContainer;
            if (potionCtrl != null)
            {
                var holdersParent = potionCtrl.GetNodeOrNull("MarginContainer/PotionHolders");
                if (holdersParent != null)
                {
                    foreach (var child in holdersParent.GetChildren())
                    {
                        if (child is Control c)
                            potionHolders.Add(c);
                    }
                }
            }

            // Collect relic holders
            var relicNodes = globalUi.RelicInventory?.RelicNodes;

            // Sync containers for position announcements
            SyncContainer(_potionContainer, potionHolders);
            SyncContainer(_relicContainer, relicNodes?.OfType<Control>());

            // Wire potions: left/right between middle elements only,
            // leave first/last edges alone (game links them to gold/room icon)
            for (int i = 0; i < potionHolders.Count; i++)
            {
                var self = potionHolders[i].GetPath();
                if (i > 0)
                    potionHolders[i].FocusNeighborLeft = potionHolders[i - 1].GetPath();
                if (i < potionHolders.Count - 1)
                    potionHolders[i].FocusNeighborRight = potionHolders[i + 1].GetPath();
                potionHolders[i].FocusNeighborTop = self;
                // Down goes to first relic if available
                if (relicNodes != null && relicNodes.Count > 0)
                {
                    var firstRelic = relicNodes.FirstOrDefault(r => r != null && GodotObject.IsInstanceValid(r));
                    if (firstRelic != null)
                        potionHolders[i].FocusNeighborBottom = firstRelic.GetPath();
                }
            }

            // Wire relics: left/right wrapping
            if (relicNodes != null && relicNodes.Count > 0)
            {
                var firstValid = relicNodes.FirstOrDefault(r => r != null && GodotObject.IsInstanceValid(r));
                var lastValid = relicNodes.LastOrDefault(r => r != null && GodotObject.IsInstanceValid(r));
                var firstPath = firstValid?.GetPath();
                var lastPath = lastValid?.GetPath();
                var firstPotionPath = potionHolders.Count > 0 ? potionHolders[0].GetPath() : null;

                foreach (var relic in relicNodes)
                {
                    if (relic == null || !GodotObject.IsInstanceValid(relic)) continue;

                    // Up goes to potions
                    if (firstPotionPath != null)
                        relic.FocusNeighborTop = firstPotionPath;

                    // Leave FocusNeighborBottom alone — game sets it to current screen content

                    // Wrap left/right
                    if (relic == firstValid && lastPath != null)
                        relic.FocusNeighborLeft = lastPath;
                    if (relic == lastValid && firstPath != null)
                        relic.FocusNeighborRight = firstPath;
                }
            }
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Top bar focus wiring failed: {e.Message}"); }
    }

    private Player? GetLocalPlayer()
    {
        if (!RunManager.Instance.IsInProgress) return null;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return null;
        return LocalContext.GetMe(runState);
    }
}
