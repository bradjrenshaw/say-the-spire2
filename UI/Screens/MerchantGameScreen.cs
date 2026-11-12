using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Screen for the merchant inventory. Registers every shop slot into
/// per-row containers (character cards, colorless cards, relics, potions)
/// so position announcements count within the row and the wrap sound's
/// structural same-container check applies — the geometric fallback
/// misread the shop's row-hopping focus wiring as wraps.
///
/// The November beta's Blitz shop reuses this inventory with different
/// contents: the usual rows are hidden and a grid of free offers is shown
/// (rows of three, as the game wires their navigation), with a line of text
/// saying how many picks remain. That line is spoken when the shop opens
/// and whenever it changes.
/// </summary>
public class MerchantGameScreen : GameScreen
{
    public static MerchantGameScreen? Current { get; private set; }

    private static readonly System.Reflection.FieldInfo CharacterCardsField =
        AccessTools.Field(typeof(NMerchantInventory), "_characterCardContainer")!;
    private static readonly System.Reflection.FieldInfo ColorlessCardsField =
        AccessTools.Field(typeof(NMerchantInventory), "_colorlessCardContainer")!;
    private static readonly System.Reflection.FieldInfo RelicsField =
        AccessTools.Field(typeof(NMerchantInventory), "_relicContainer")!;
    private static readonly System.Reflection.FieldInfo PotionsField =
        AccessTools.Field(typeof(NMerchantInventory), "_potionContainer")!;
    private static readonly System.Reflection.FieldInfo CardRemovalField =
        AccessTools.Field(typeof(NMerchantInventory), "_cardRemovalNode")!;
    private static readonly System.Reflection.FieldInfo BackButtonField =
        AccessTools.Field(typeof(NMerchantInventory), "_backButton")!;

    // Beta-only (Blitz shop); null on stable.
    private static readonly System.Reflection.FieldInfo? BlitzSlotsField =
        AccessTools.Field(typeof(NMerchantInventory), "_blitzContainer");
    private static readonly System.Reflection.FieldInfo? BlitzSelectionTextField =
        AccessTools.Field(typeof(NMerchantInventory), "_blitzSelectionText");

    // The game lays the Blitz offers out, and wires their focus, three to a row.
    private const int BlitzRowSize = 3;

    private readonly NMerchantInventory _screen;
    private readonly ListContainer _root;
    private string? _lastBlitzSelectionText;

    public override Message? ScreenName => Message.Localized("ui", "EVENT.ROOM_SHOP");

    public MerchantGameScreen(NMerchantInventory screen)
    {
        _screen = screen;
        _root = new ListContainer
        {
            ContainerLabel = ScreenName,
            AnnounceName = true,
            AnnouncePosition = false,
        };
        RootElement = _root;
    }

    public override void OnPush()
    {
        Current = this;
        base.OnPush();
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
        _root.Clear();
        _connectedControls.Clear();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (!GodotObject.IsInstanceValid(_screen))
            return;
        if (BlitzSelectionTextField?.GetValue(_screen) is not RichTextLabel { Visible: true } label)
            return;

        // "You may choose N more items." — changes after each pick, and when
        // an offer grants extra picks.
        var text = label.Text;
        if (text == _lastBlitzSelectionText)
            return;
        _lastBlitzSelectionText = text;
        if (!string.IsNullOrWhiteSpace(text))
            SpeechManager.Output(Message.Raw(ProxyElement.StripBbcode(text)));
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterRow(CharacterCardsField);
        RegisterRow(ColorlessCardsField);
        RegisterRow(RelicsField);
        RegisterRow(PotionsField);
        RegisterBlitzRows();

        // Single slots outside the rows.
        if (CardRemovalField.GetValue(_screen) is NMerchantSlot { Visible: true } removal)
            RegisterSlot(removal, _root);
        if (BackButtonField.GetValue(_screen) is Control backButton)
        {
            var proxy = ProxyFactory.Create(backButton);
            _root.Add(proxy);
            Register(backButton, proxy);
        }
    }

    private void RegisterRow(System.Reflection.FieldInfo containerField)
    {
        // The game hides a row's container when the shop has nothing for it
        // (every regular row, in a Blitz shop).
        if (containerField.GetValue(_screen) is not Control { Visible: true } container)
            return;

        RegisterSlots(container.GetChildren().OfType<NMerchantSlot>().ToList());
    }

    private void RegisterBlitzRows()
    {
        if (BlitzSlotsField?.GetValue(_screen) is not Control { Visible: true } container)
            return;

        var slots = container.GetChildren().OfType<NMerchantSlot>().Where(s => s.Visible);
        foreach (var rowSlots in slots.Chunk(BlitzRowSize))
            RegisterSlots(rowSlots);
    }

    private void RegisterSlots(IReadOnlyCollection<NMerchantSlot> slots)
    {
        if (slots.Count == 0)
            return;

        var row = new ListContainer
        {
            AnnounceName = false,
            AnnouncePosition = true,
        };
        _root.Add(row);
        foreach (var slot in slots)
            RegisterSlot(slot, row);
    }

    private void RegisterSlot(NMerchantSlot slot, ListContainer row)
    {
        var proxy = new ProxyMerchantSlot(slot);
        row.Add(proxy);
        Register(slot, proxy);
    }
}
