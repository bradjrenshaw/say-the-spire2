using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
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
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.Views;

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

        // Yield rewire during card-play and targeting. Cancel is handled via
        // a Harmony prefix on NTopBarPauseButton.OnRelease (PauseButtonHooks)
        // and confirm via TryConfirmActiveCardPlay below — both bypass the
        // game's _Input propagation, which doesn't reliably receive our
        // injected InputEventAction.
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if ((hand != null && hand.InCardPlay) || _isTargeting)
            return;

        // Rewire the combat focus chain every frame. We previously gated this
        // behind a state-fold "signature" to skip unchanged frames, but the
        // combat UI mutates its controls in too many subtle ways for a hand-
        // maintained signature to track reliably — missed changes left the
        // focus wiring stale (orbs unreadable, neighbors pointing at freed
        // nodes). Rewiring unconditionally is simpler and correct; it was not
        // the real source of the combat lag.
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
            case "ui_select":
                // Same beta bug applies to confirm: prefer playing the centered
                // card directly when card-play is active; only fall through to
                // the expanded-state toggle otherwise.
                if (TryConfirmActiveCardPlay()) return true;
                return TryOpenPlayerExpandedState();
        }

        return false;
    }

    /// <summary>
    /// Direct confirm for an active card play that doesn't need a target
    /// (Defend / self / no-target cards). Mirrors what the Confirmed-signal
    /// handler in <c>NControllerCardPlay.MultiCreatureTargeting</c> does
    /// (<c>EnableControllerNavigation</c> + <c>TryPlayCard(null)</c>) via the
    /// signal so subscribers in the multi-creature targeting setup run. Single-
    /// target cards (AnyEnemy/AnyAlly) drive their confirm through
    /// NTargetManager and are left alone here — focus on the targeted creature
    /// + Enter still goes through the game's own path for them.
    /// </summary>
    private static bool TryConfirmActiveCardPlay()
    {
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand == null || !hand.InCardPlay) return false;
        if (CurrentCardPlayField?.GetValue(hand) is not NControllerCardPlay cardPlay
            || !GodotObject.IsInstanceValid(cardPlay)) return false;

        // Only intercept for no-target cards. Single-target (AnyEnemy/AnyAlly)
        // routes confirmation through NTargetManager and the focused creature.
        var card = CardPlayCardGetter?.Invoke(cardPlay, null) as CardModel;
        var targetType = card?.TargetType;
        if (targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly)
            return false;

        cardPlay.EmitSignal(NControllerCardPlay.SignalName.Confirmed);
        return true;
    }

    private void AnnounceCombatantStatus(int index)
    {
        var creature = GetBoundCombatant(index);
        if (creature == null)
            return;

        var view = CreatureView.FromEntity(creature);
        var parts = new List<Message> { Message.Raw(view.Name) };

        var owner = OwnerMessage(view);
        if (owner != null)
            parts.Add(owner);

        parts.Add(Message.Raw(view.CurrentHp.ToString()));

        if (view.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = view.Block }));

        SpeechManager.Output(Message.Join(", ", parts.ToArray()));
    }

    private void AnnounceCombatantIntent(int index)
    {
        var creature = GetBoundCombatant(index);
        if (creature == null)
            return;

        var intent = GetCombatantIntentSummary(creature);
        if (intent.IsEmpty)
            return;

        var view = CreatureView.FromEntity(creature);
        var owner = OwnerMessage(view);
        if (owner != null)
            intent = Message.Join(", ", Message.Raw(view.Name), owner, intent);

        SpeechManager.Output(intent);
    }

    private static Message? OwnerMessage(CreatureView view)
    {
        var owner = view.OtherPlayerPetOwner;
        return owner == null
            ? null
            : Message.Localized("ui", "MULTIPLAYER.PET_OWNER",
                new { owner = Multiplayer.MultiplayerHelper.GetPlayerName(owner) });
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

    private Message GetCombatantIntentSummary(Creature creature)
    {
        return CreatureIntentFormatter.Summary(CreatureView.FromEntity(creature), includePrefix: false)
            ?? Message.Localized("ui", "SPEECH.NO_INTENT");
    }

    private void AnnounceBlock()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        UI.Announcements.HotkeyAnnouncementRegistry.Announce(
            "announce_block", new UI.Announcements.BlockAnnouncement(player.Creature.Block));
    }

    private void AnnounceEnergy()
    {
        var player = GetLocalPlayer();
        var combatState = player?.PlayerCombatState;
        if (combatState == null) return;
        UI.Announcements.HotkeyAnnouncementRegistry.Announce(
            "announce_energy", new UI.Announcements.ResourcesAnnouncement(combatState));
    }

    private void AnnouncePowers()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        UI.Announcements.HotkeyAnnouncementRegistry.Announce(
            "announce_powers", new UI.Announcements.PowersAnnouncement(player.Creature.Powers));
    }

    private void AnnounceIntents()
    {
        var state = GetLiveState();
        if (state == null) return;

        var enemies = new List<(string, IReadOnlyList<Views.IntentView>)>();
        foreach (var enemy in state.Enemies)
        {
            if (!enemy.IsAlive || !enemy.IsMonster) continue;
            enemies.Add((enemy.Name, CreatureView.FromEntity(enemy).MonsterIntents));
        }

        UI.Announcements.HotkeyAnnouncementRegistry.Announce(
            "announce_intents", new UI.Announcements.AllIntentsAnnouncement(enemies));
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

        UI.Announcements.HotkeyAnnouncementRegistry.Announce(
            "announce_summarized_intents", new UI.Announcements.IncomingDamageAnnouncement(totalDamage));
    }

    /// <summary>
    /// If the currently focused creature is a player, simulate clicking
    /// the corresponding NMultiplayerPlayerState hitbox to open the expanded view.
    /// Returns true if handled, false to let the action propagate.
    /// </summary>
    private bool TryOpenPlayerExpandedState()
    {
        // While the game has targeting active (potion target, single-target
        // card aim) the focused creature is the target candidate. Letting Enter
        // open the player expanded state here would consume the action, our
        // dispatch would suppress propagation, and the game's NTargetManager
        // would never see the confirm — the potion / card just wouldn't throw.
        if (_isTargeting) return false;

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

            // The game makes summoned pets non-interactable when they belong to
            // remote players (NCombatRoom.OnCreatureNodeCreated → ToggleIsInteractable(false)),
            // which both strips them of focusability and excludes them from the
            // left/right wraparound chain in NCombatRoom.UpdateCreatureNavigation.
            // Force their focus mode back to All and rebuild the chain to include
            // them, ordered by X position so dpad-left/right walks the row visually.
            // Skipped during targeting because the game's targeting whitelist
            // legitimately needs to lock focus to valid targets only.
            if (!_isTargeting && Settings.UIEnhancementsSettings.KeepSummonsFocusable.Get())
            {
                foreach (var creature in combatRoom.CreatureNodes)
                {
                    if (creature?.Hitbox == null) continue;
                    if (creature.Entity.PetOwner == null) continue;
                    if (creature.Entity.IsDead) continue;
                    creature.Hitbox.FocusMode = Control.FocusModeEnum.All;
                }

                var chain = combatRoom.CreatureNodes
                    .Where(c => c?.Hitbox != null && !c.Entity.IsDead
                        && c.Hitbox.FocusMode == Control.FocusModeEnum.All)
                    .OrderBy(c => c.Hitbox.GlobalPosition.X)
                    .ToList();

                for (int i = 0; i < chain.Count; i++)
                {
                    var hitbox = chain[i].Hitbox;
                    var left = i > 0 ? chain[i - 1] : chain[^1];
                    var right = i < chain.Count - 1 ? chain[i + 1] : chain[0];
                    hitbox.FocusNeighborLeft = left.Hitbox.GetPath();
                    hitbox.FocusNeighborRight = right.Hitbox.GetPath();
                }
            }

            if (!Settings.UIEnhancementsSettings.Combat.Get())
                return;

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

    private static readonly FieldInfo OrbManagerOrbsField =
        AccessTools.Field(typeof(NOrbManager), "_orbs");

    private static readonly FieldInfo CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static readonly MethodInfo CardPlayCardGetter =
        AccessTools.PropertyGetter(typeof(NCardPlay), "Card");

    /// <summary>
    /// The local player's orb-slot controls, read from the game's logical
    /// <c>NOrbManager._orbs</c> list rather than the "%Orbs" scene node.
    /// The game populates <c>_orbs</c> synchronously the moment an orb is
    /// channeled and wires controller-nav from it (<c>UpdateControllerNavigation</c>
    /// uses <c>_orbs[i].GetPath()</c>), but the actual reparenting into "%Orbs"
    /// happens through deferred <c>AddChildSafely</c>/<c>RemoveChildSafely</c>
    /// calls that don't settle until the turn transition. Reading the tree node
    /// therefore missed the orb for the whole grant turn; reading <c>_orbs</c>
    /// sees it the same frame the game does. The slots are real, tree-attached
    /// controls (focusable mid-turn), just not yet under "%Orbs".
    /// </summary>
    private static List<Control> GetLocalPlayerOrbs(NCombatRoom combatRoom)
    {
        var orbs = new List<Control>();
        foreach (var c in combatRoom.CreatureNodes)
        {
            if (c?.OrbManager == null || !c.Entity.IsPlayer
                || !LocalContext.IsMe(c.Entity))
                continue;
            if (OrbManagerOrbsField.GetValue(c.OrbManager) is IEnumerable<NOrb> list)
            {
                foreach (var orb in list)
                    if (GodotObject.IsInstanceValid(orb)) orbs.Add(orb);
            }
            break;
        }
        return orbs;
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

        // Orbs — read the local player's orb slots from the game's logical
        // NOrbManager._orbs list (see GetLocalPlayerOrbs). Non-Defect
        // characters get a slot here only when a card grants them an orb
        // mid-combat, and the orb lands in _orbs before it reparents into the
        // "%Orbs" scene node, so we must read the logical list to catch it.
        var orbNodes = GetLocalPlayerOrbs(combatRoom);

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
                .OrderBy(c => c.Hitbox?.GetScreenPosition().X ?? c.GlobalPosition.X)
                .OfType<Control>());

        // Creatures are wired so Godot focuses the Hitbox (NClickableControl),
        // not the NCreature itself. Without this mapping, focus events on the
        // hitbox fall through ProxyFactory and produce an unparented
        // ProxyCreature, breaking home / end navigation. Map the hitbox to
        // the same cached element so GetElement(hitbox) returns the parented
        // entry from _creatureContainer.
        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature?.Hitbox != null && _elementCache.TryGetValue(creature, out var element))
                _elementCache[creature.Hitbox] = element;
        }

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
        creature.PowerApplied += handlers.OnPowerApplied;
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
            creature.PowerApplied -= handlers.OnPowerApplied;
            creature.PowerIncreased -= handlers.OnPowerIncreased;
            creature.PowerDecreased -= handlers.OnPowerDecreased;
            creature.PowerRemoved -= handlers.OnPowerRemoved;
            creature.Died -= handlers.OnDied;
        }
        _subscribedCreatures.Clear();
    }

}
