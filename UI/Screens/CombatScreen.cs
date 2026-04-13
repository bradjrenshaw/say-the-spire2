using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CombatScreen : Screen
{
    public static CombatScreen? Current { get; private set; }
    private static readonly string[] CombatantSelectActions =
    {
        "mega_select_card_1",
        "mega_select_card_2",
        "mega_select_card_3",
        "mega_select_card_4",
        "mega_select_card_5",
        "mega_select_card_6",
        "mega_select_card_7",
        "mega_select_card_8",
        "mega_select_card_9",
        "mega_select_card_10",
        "mega_select_card_11",
        "mega_select_card_12",
    };

    private static readonly string[] CombatantIntentActions =
    {
        "announce_combatant_intent_1",
        "announce_combatant_intent_2",
        "announce_combatant_intent_3",
        "announce_combatant_intent_4",
        "announce_combatant_intent_5",
        "announce_combatant_intent_6",
        "announce_combatant_intent_7",
        "announce_combatant_intent_8",
        "announce_combatant_intent_9",
        "announce_combatant_intent_10",
        "announce_combatant_intent_11",
        "announce_combatant_intent_12",
    };

    private static readonly string[] _alwaysEnabled = { "events" };
    public override IEnumerable<string> AlwaysEnabledBuffers => _alwaysEnabled;

    private CombatState? _currentState;
    private readonly Dictionary<Creature, CombatCreatureHandlers> _subscribedCreatures = new();
    private CombatCardPileHandlers? _cardPileHandlers;

    // Containers for position announcements (no labels, position only)
    private readonly ListContainer _rootContainer = new() { AnnounceName = false, AnnouncePosition = false };
    private readonly ListContainer _potionContainer = new() { AnnounceName = false };
    private readonly ListContainer _relicContainer = new() { AnnounceName = false };
    private readonly ListContainer _orbContainer = new() { AnnounceName = false };
    private readonly ListContainer _creatureContainer = new() { AnnounceName = false };
    private readonly ListContainer _handContainer = new() { AnnounceName = false };
    private readonly Dictionary<Control, UIElement> _elementCache = new();

    public CombatScreen()
    {
        ClaimAction("announce_block");
        ClaimAction("announce_energy");
        ClaimAction("announce_powers");
        ClaimAction("announce_intents");
        ClaimAction("announce_summarized_intents");
        ClaimAction("ui_accept", propagate: true);
        ClaimAction("ui_select", propagate: true);
        foreach (var action in CombatantSelectActions)
            ClaimAction(action);
        foreach (var action in CombatantIntentActions)
            ClaimAction(action);

        _rootContainer.Add(_potionContainer);
        _rootContainer.Add(_relicContainer);
        _rootContainer.Add(_orbContainer);
        _rootContainer.Add(_creatureContainer);
        _rootContainer.Add(_handContainer);
        RootElement = _rootContainer;
    }

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.COMBAT_NAV", "Use directional controls to navigate between creatures and your hand. Press Enter on a card to play it. For cards that don't require a target, press Enter a second time to confirm."), exclusive: true),
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.COMBAT_TOP_PANEL", "Use your Top Panel key to quickly jump to your potions and relics."), exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.END_TURN", "End Turn"), "ui_accept", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_BLOCK", "Announce Block"), "announce_block", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_ENERGY", "Announce Energy"), "announce_energy", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_POWERS", "Announce Powers"), "announce_powers", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_INTENTS", "Announce Intents"), "announce_intents", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_SUMMARIZED_INTENTS", "Announce Summarized Intents"), "announce_summarized_intents", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.VIEW_DRAW_PILE", "View Draw Pile"), "mega_view_draw_pile", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.VIEW_DISCARD_PILE", "View Discard Pile"), "mega_view_discard_pile", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.VIEW_EXHAUST_PILE", "View Exhaust Pile"), "mega_view_exhaust_pile_and_tab_right", exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.SELECT_COMBATANT", "Select Combatant 1-12"), CombatantSelectActions, exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.ANNOUNCE_COMBATANT_INTENT", "Announce Combatant Intent 1-12"), CombatantIntentActions, exclusive: true),
    };

    private CombatState? GetLiveState()
    {
        return CombatManager.Instance?.DebugOnlyGetState();
    }

    public override void OnPush()
    {
        Current = this;
        Log.Info($"[EventDebug] CombatScreen.OnPush: this={GetHashCode()}");
        CombatManager.Instance.CreaturesChanged += OnCreaturesChanged;
        CombatManager.Instance.TurnStarted += OnTurnStarted;

        var state = GetLiveState();
        if (state != null)
            SubscribeToState(state);

        Log.Info("[AccessibilityMod] CombatScreen pushed.");
    }

    public override void OnPop()
    {
        Log.Info($"[EventDebug] CombatScreen.OnPop: this={GetHashCode()}");
        var cm = CombatManager.Instance;
        if (cm != null)
        {
            cm.CreaturesChanged -= OnCreaturesChanged;
            cm.TurnStarted -= OnTurnStarted;
        }
        UnsubscribeFromState();
        _elementCache.Clear();
        _potionContainer.Clear();
        _relicContainer.Clear();
        _orbContainer.Clear();
        _creatureContainer.Clear();
        _handContainer.Clear();
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] CombatScreen popped.");
    }

    public override void OnUpdate()
    {
        var liveState = GetLiveState();
        if (liveState != null && liveState != _currentState)
        {
            Log.Info($"[EventDebug] CombatScreen: CombatState changed, resubscribing");
            UnsubscribeFromState();
            SubscribeToState(liveState);
        }

        UpdateFocusNavigation();
    }

    private void SubscribeToState(CombatState state)
    {
        _currentState = state;
        SubscribeToAllCreatures(state);

        var player = GetLocalPlayer();
        if (player?.PlayerCombatState != null)
        {
            _cardPileHandlers = new CombatCardPileHandlers(player.PlayerCombatState);
            _cardPileHandlers.Subscribe();
            Log.Info("[EventDebug] CombatCardPileHandlers subscribed.");
        }

        CombatManager.Instance.PlayerEndedTurn += OnPlayerEndedTurn;
        CombatManager.Instance.PlayerUnendedTurn += OnPlayerUnendedTurn;
    }

    private void UnsubscribeFromState()
    {
        _cardPileHandlers?.Unsubscribe();
        _cardPileHandlers = null;

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.PlayerEndedTurn -= OnPlayerEndedTurn;
            CombatManager.Instance.PlayerUnendedTurn -= OnPlayerUnendedTurn;
        }

        UnsubscribeAll();
        _currentState = null;
    }

    // -- Shortcut announcements --

    public override bool OnActionJustPressed(InputAction action)
    {
        var combatantIndex = System.Array.IndexOf(CombatantSelectActions, action.Key);
        if (combatantIndex >= 0)
        {
            AnnounceCombatantStatus(combatantIndex);
            return true;
        }

        var intentIndex = System.Array.IndexOf(CombatantIntentActions, action.Key);
        if (intentIndex >= 0)
        {
            AnnounceCombatantIntent(intentIndex);
            return true;
        }

        switch (action.Key)
        {
            case "announce_block":
                AnnounceBlock();
                return true;
            case "announce_energy":
                AnnounceEnergy();
                return true;
            case "announce_powers":
                AnnouncePowers();
                return true;
            case "announce_intents":
                AnnounceIntents();
                return true;
            case "announce_summarized_intents":
                AnnounceSummarizedIntents();
                return true;
            case "ui_accept":
            case "ui_select":
                return TryOpenPlayerExpandedState();
        }

        return false;
    }

    private void AnnounceCombatantStatus(int index)
    {
        var creature = GetBoundCombatant(index);
        if (creature == null)
            return;

        var parts = new List<string>
        {
            Multiplayer.MultiplayerHelper.GetCreatureName(creature),
            creature.CurrentHp.ToString()
        };

        if (creature.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = creature.Block }).Resolve());

        SpeechManager.Output(Message.Raw(string.Join(", ", parts)));
    }

    private void AnnounceCombatantIntent(int index)
    {
        var creature = GetBoundCombatant(index);
        if (creature == null)
            return;

        var intent = GetCombatantIntentSummary(creature);
        if (string.IsNullOrEmpty(intent))
            return;

        SpeechManager.Output(Message.Raw(intent));
    }

    private Creature? GetBoundCombatant(int index)
    {
        if (index < 0)
            return null;

        var state = GetLiveState();
        if (state == null)
            return null;

        var combatants = state.Enemies
            .Concat(state.Allies)
            .Where(c => c != null && c.IsAlive && !LocalContext.IsMe(c))
            .Take(CombatantSelectActions.Length)
            .ToList();

        if (index >= combatants.Count)
            return null;

        return combatants[index];
    }

    private string? GetCombatantIntentSummary(Creature creature)
    {
        return ProxyCreature.GetIntentSummary(creature, includePrefix: false) ?? LocalizationManager.GetOrDefault("ui", "SPEECH.NO_INTENT", "No intent");
    }

    private void AnnounceBlock()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = player.Creature.Block }));
    }

    private void AnnounceEnergy()
    {
        var player = GetLocalPlayer();
        var combatState = player?.PlayerCombatState;
        if (combatState == null) return;
        SpeechManager.Output(Message.Raw(ResourceHelper.GetResourceString(combatState)));
    }

    private void AnnouncePowers()
    {
        var player = GetLocalPlayer();
        if (player == null) return;

        var powers = player.Creature.Powers;
        if (powers.Count == 0)
        {
            SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_POWERS"));
            return;
        }

        var sb = new StringBuilder();
        foreach (var power in powers)
        {
            if (sb.Length > 0) sb.Append(", ");
            var title = power.Title.GetFormattedText();
            if (power.DisplayAmount != 0)
                sb.Append($"{title} {power.DisplayAmount}");
            else
                sb.Append(title);
        }
        SpeechManager.Output(Message.Raw(sb.ToString()));
    }

    private void AnnounceIntents()
    {
        var state = GetLiveState();
        if (state == null) return;

        var allies = state.Allies;

        var sb = new StringBuilder();
        foreach (var enemy in state.Enemies)
        {
            if (!enemy.IsAlive || !enemy.IsMonster) continue;

            if (sb.Length > 0) sb.Append(". ");
            sb.Append(enemy.Name);
            sb.Append(": ");

            var move = enemy.Monster!.NextMove;
            var intentParts = new List<string>();
            foreach (var intent in move.Intents)
            {
                var name = ProxyCreature.GetIntentName(intent);
                var label = intent.GetIntentLabel(allies, enemy).GetFormattedText();
                if (!string.IsNullOrEmpty(label) && label != "")
                    intentParts.Add($"{name} {label}");
                else
                    intentParts.Add(name);
            }
            sb.Append(intentParts.Count > 0 ? string.Join(", ", intentParts) : LocalizationManager.GetOrDefault("ui", "LABELS.UNKNOWN", "Unknown"));
        }

        if (sb.Length == 0)
        {
            SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_ENEMIES"));
            return;
        }

        SpeechManager.Output(Message.Raw(sb.ToString()));
    }

    private void AnnounceSummarizedIntents()
    {
        var state = GetLiveState();
        if (state == null) return;

        var allies = state.Allies;
        int totalDamage = 0;

        foreach (var enemy in state.Enemies)
        {
            if (!enemy.IsAlive || !enemy.IsMonster) continue;

            var move = enemy.Monster!.NextMove;
            foreach (var intent in move.Intents)
            {
                if (intent is AttackIntent attackIntent)
                    totalDamage += attackIntent.GetTotalDamage(allies, enemy);
            }
        }

        SpeechManager.Output(totalDamage > 0
            ? Message.Localized("ui", "SPEECH.INCOMING_DAMAGE", new { amount = totalDamage })
            : Message.Localized("ui", "SPEECH.NO_INCOMING_DAMAGE"));
    }

    /// <summary>
    /// If the currently focused creature is a player, simulate clicking
    /// the corresponding NMultiplayerPlayerState hitbox to open the expanded view.
    /// Returns true if handled, false to let the action propagate.
    /// </summary>
    private bool TryOpenPlayerExpandedState()
    {
        try
        {
            // Find the currently focused creature
            var viewport = NRun.Instance?.GetViewport();
            var focused = viewport?.GuiGetFocusOwner();
            if (focused == null) return false;

            // Walk up to find the NCreature
            NCreature? creature = focused as NCreature;
            var current = focused.GetParent();
            while (creature == null && current != null)
            {
                if (current is NCreature nc)
                    creature = nc;
                current = current.GetParent();
            }
            if (creature == null || !creature.Entity.IsPlayer) return false;

            // Find the matching NMultiplayerPlayerState
            var container = NRun.Instance?.GlobalUi?.MultiplayerPlayerContainer;
            if (container == null) return false;

            foreach (var child in container.GetChildren())
            {
                if (child is NMultiplayerPlayerState state && state.Player == creature.Entity.Player)
                {
                    // Simulate clicking the hitbox
                    state.Hitbox.EmitSignal(NClickableControl.SignalName.Released, state.Hitbox);
                    return true;
                }
            }
        }
        catch (System.Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[AccessibilityMod] TryOpenPlayerExpandedState error: {e.Message}");
        }
        return false;
    }

    private void OnPlayerEndedTurn(Player player, bool canBackOut)
    {
        var name = Multiplayer.MultiplayerHelper.GetPlayerDisplayName(player);
        EventDispatcher.Enqueue(new EndTurnEvent(name, ready: true, player.Creature));
    }

    private void OnPlayerUnendedTurn(Player player)
    {
        var name = Multiplayer.MultiplayerHelper.GetPlayerDisplayName(player);
        EventDispatcher.Enqueue(new EndTurnEvent(name, ready: false, player.Creature));
    }

    private Player? GetLocalPlayer()
    {
        if (!RunManager.Instance.IsInProgress) return null;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return null;
        return LocalContext.GetMe(runState);
    }

    // -- Navigation (frame-driven) --

    private bool _isTargeting;

    public void OnTargetingStarted() => _isTargeting = true;
    public void OnTargetingFinished() => _isTargeting = false;

    public void OnCardStolen(string cardName)
    {
        EventDispatcher.Enqueue(new CardStolenEvent(cardName));
    }

    public override UIElement? GetElement(Control control)
    {
        return _elementCache.TryGetValue(control, out var element) ? element : null;
    }

    private UIElement GetOrCreateElement(Control control)
    {
        if (_elementCache.TryGetValue(control, out var element))
            return element;
        element = ProxyFactory.Create(control);
        _elementCache[control] = element;
        return element;
    }

    private void SyncContainer(ListContainer container, IEnumerable<Control>? controls)
    {
        container.Clear();
        if (controls == null) return;
        foreach (var control in controls)
        {
            if (control != null && GodotObject.IsInstanceValid(control))
                container.Add(GetOrCreateElement(control));
        }
    }

    /// <summary>
    /// Rebuilds the full focus chain every frame. Every item in every row
    /// gets explicit up/down links to the adjacent rows.
    ///
    /// Rows (bottom to top):
    ///   hand cards ↔ creatures ↔ orbs (if Defect has orbs) ↔ relics
    ///
    /// During targeting, we leave FocusMode alone and lock creatures' nav.
    /// </summary>
    private void UpdateFocusNavigation()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            var (potionHolders, relicNodes, orbNodes, hand) = CollectFocusRows(combatRoom);

            bool hasOrbs = orbNodes.Count > 0;
            NodePath? firstOrbPath = hasOrbs ? orbNodes[0].GetPath() : null;

            SyncAllContainers(combatRoom, potionHolders, relicNodes, orbNodes, hand);

            if (_isTargeting)
            {
                // During targeting, lock creatures to their own row
                foreach (var creature in combatRoom.CreatureNodes)
                {
                    if (creature?.Hitbox == null) continue;
                    var hitbox = creature.Hitbox;
                    hitbox.FocusNeighborTop = hitbox.GetPath();
                    hitbox.FocusNeighborBottom = hitbox.GetPath();
                }
                return;
            }

            WireFocusNeighbors(combatRoom, relicNodes, orbNodes, hand, hasOrbs, firstOrbPath);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] UpdateFocusNavigation failed: {e.Message}");
        }
    }

    private static (List<Control> potions, List<Control>? relics, List<Control> orbs, NPlayerHand? hand)
        CollectFocusRows(NCombatRoom combatRoom)
    {
        // Potions
        var potionHolders = new List<Control>();
        var potionCtrl = NRun.Instance?.GlobalUi?.TopBar?.PotionContainer;
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

        // Relics
        var relicNodes = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.OfType<Control>().ToList();

        // Orbs — collect from local player's orb manager
        var orbNodes = new List<Control>();
        foreach (var c in combatRoom.CreatureNodes)
        {
            if (c != null && c.Entity.IsPlayer && c.OrbManager != null
                && MegaCrit.Sts2.Core.Context.LocalContext.IsMe(c.Entity))
            {
                var defaultOwner = c.OrbManager.DefaultFocusOwner;
                if (defaultOwner != null && defaultOwner != c.Hitbox)
                {
                    foreach (var child in c.OrbManager.GetChildren())
                    {
                        foreach (var orb in child.GetChildren())
                        {
                            if (orb is Control orbCtrl)
                                orbNodes.Add(orbCtrl);
                        }
                    }
                }
                break;
            }
        }

        return (potionHolders, relicNodes, orbNodes, combatRoom.Ui?.Hand);
    }

    private void SyncAllContainers(NCombatRoom combatRoom,
        List<Control> potionHolders, List<Control>? relicNodes,
        List<Control> orbNodes, NPlayerHand? hand)
    {
        SyncContainer(_potionContainer, potionHolders);
        SyncContainer(_relicContainer, relicNodes);
        orbNodes.Reverse();
        SyncContainer(_orbContainer, orbNodes);
        SyncContainer(_creatureContainer,
            combatRoom.CreatureNodes
                .Where(c => c != null && (!_isTargeting
                    || (c.Hitbox != null && c.Hitbox.FocusMode != Control.FocusModeEnum.None)))
                .OrderBy(c => c.GlobalPosition.X)
                .OfType<Control>());
        SyncContainer(_handContainer, hand?.ActiveHolders?.OfType<Control>());
    }

    private static void WireFocusNeighbors(NCombatRoom combatRoom,
        List<Control>? relicNodes, List<Control> orbNodes,
        NPlayerHand? hand, bool hasOrbs, NodePath? firstOrbPath)
    {
        // Resolve first-element paths for each row
        NodePath? firstRelicPath = null;
        if (relicNodes != null && relicNodes.Count > 0)
        {
            var first = relicNodes[0];
            if (GodotObject.IsInstanceValid(first))
                firstRelicPath = first.GetPath();
        }

        var firstCreature = combatRoom.CreatureNodes
            .FirstOrDefault(c => c != null && c.Hitbox != null);
        NodePath? firstCreaturePath = firstCreature?.Hitbox?.GetPath();

        NodePath? firstHandPath = null;
        if (hand != null)
        {
            var firstHolder = hand.ActiveHolders.FirstOrDefault();
            if (firstHolder != null)
                firstHandPath = firstHolder.GetPath();
        }

        // Relics: ↑ = (game links to potions), ↓ = orbs or creatures, left/right wraps
        NodePath? relicDown = hasOrbs ? firstOrbPath : firstCreaturePath;
        if (relicNodes != null && relicNodes.Count > 0)
        {
            var firstValid = relicNodes.FirstOrDefault(r => r != null && GodotObject.IsInstanceValid(r));
            var lastValid = relicNodes.LastOrDefault(r => r != null && GodotObject.IsInstanceValid(r));
            NodePath? firstPath = firstValid?.GetPath();
            NodePath? lastPath = lastValid?.GetPath();

            foreach (var relic in relicNodes)
            {
                if (relic == null || !GodotObject.IsInstanceValid(relic)) continue;
                if (relicDown != null)
                    relic.FocusNeighborBottom = relicDown;
                if (relic == firstValid && lastPath != null)
                    relic.FocusNeighborLeft = lastPath;
                if (relic == lastValid && firstPath != null)
                    relic.FocusNeighborRight = firstPath;
            }
        }

        // Orbs: ↑ = relics, ↓ = creatures
        if (hasOrbs)
        {
            foreach (var orb in orbNodes)
            {
                if (firstRelicPath != null)
                    orb.FocusNeighborTop = firstRelicPath;
                if (firstCreaturePath != null)
                    orb.FocusNeighborBottom = firstCreaturePath;
            }
        }

        // Creatures: ↑ = orbs or relics, ↓ = hand
        NodePath? creatureUp = hasOrbs ? firstOrbPath : firstRelicPath;
        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature?.Hitbox == null) continue;
            var hitbox = creature.Hitbox;
            hitbox.FocusMode = Control.FocusModeEnum.All;
            if (creatureUp != null)
                hitbox.FocusNeighborTop = creatureUp;
            if (firstHandPath != null)
                hitbox.FocusNeighborBottom = firstHandPath;
        }

        // Hand: ↑ = creatures
        if (hand != null && firstCreaturePath != null)
        {
            foreach (var holder in hand.ActiveHolders)
            {
                if (holder != null)
                    holder.FocusNeighborTop = firstCreaturePath;
            }
        }
    }

    // -- Combat event subscriptions --

    private void OnCreaturesChanged(CombatState state)
    {
        SubscribeToAllCreatures(state);
    }

    private void OnTurnStarted(CombatState state)
    {
        _cardPileHandlers?.OnTurnStarted();
        EventDispatcher.Enqueue(new TurnEvent(state.CurrentSide, state.RoundNumber, isStart: true));
    }

    public void OnShuffleStarting() => _cardPileHandlers?.OnShuffleStarting();
    public void OnShuffleStarted(Task shuffleTask) => _cardPileHandlers?.OnShuffleStarted(shuffleTask);

    private void SubscribeToAllCreatures(CombatState state)
    {
        foreach (var creature in state.Creatures)
        {
            if (_subscribedCreatures.ContainsKey(creature)) continue;
            SubscribeToCreature(creature);
        }
    }

    private void SubscribeToCreature(Creature creature)
    {
        var handlers = new CombatCreatureHandlers(creature);
        _subscribedCreatures[creature] = handlers;

        creature.BlockChanged += handlers.OnBlockChanged;
        creature.CurrentHpChanged += handlers.OnCurrentHpChanged;
        creature.PowerIncreased += handlers.OnPowerIncreased;
        creature.PowerDecreased += handlers.OnPowerDecreased;
        creature.PowerRemoved += handlers.OnPowerRemoved;
        creature.Died += handlers.OnDied;

        Log.Info($"[EventDebug] Subscribed to creature: {creature.Name} (alive={creature.IsAlive}, total subs={_subscribedCreatures.Count})");
    }

    private void UnsubscribeAll()
    {
        foreach (var (creature, handlers) in _subscribedCreatures)
        {
            creature.BlockChanged -= handlers.OnBlockChanged;
            creature.CurrentHpChanged -= handlers.OnCurrentHpChanged;
            creature.PowerIncreased -= handlers.OnPowerIncreased;
            creature.PowerDecreased -= handlers.OnPowerDecreased;
            creature.PowerRemoved -= handlers.OnPowerRemoved;
            creature.Died -= handlers.OnDied;
        }
        _subscribedCreatures.Clear();
    }

}
