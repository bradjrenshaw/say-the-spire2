using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Screen for the merchant inventory. Registers every shop slot into
/// per-row containers (character cards, colorless cards, relics, potions)
/// so position announcements count within the row and the wrap sound's
/// structural same-container check applies — the geometric fallback
/// misread the shop's row-hopping focus wiring as wraps.
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

    private readonly NMerchantInventory _screen;
    private readonly ListContainer _root;

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

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterRow(CharacterCardsField);
        RegisterRow(ColorlessCardsField);
        RegisterRow(RelicsField);
        RegisterRow(PotionsField);

        // Single slots outside the rows.
        if (CardRemovalField.GetValue(_screen) is NMerchantSlot removal)
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
        if (containerField.GetValue(_screen) is not Control container)
            return;

        var slots = container.GetChildren().OfType<NMerchantSlot>().ToList();
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
