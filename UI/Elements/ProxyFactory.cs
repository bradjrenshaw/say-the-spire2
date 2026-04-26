using Godot;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

namespace SayTheSpire2.UI.Elements;

public static class ProxyFactory
{
    public static ProxyElement Create(Control control)
    {
        // Most specific types first
        if (control is NEpochSlot)
            return new ProxyEpochSlot(control);

        if (control is NEventOptionButton)
            return new ProxyEventOptionButton(control);

        if (control is NCharacterSelectButton)
            return new ProxyCharacterButton(control);

        if (control is NInputSettingsEntry)
            return new ProxyInputBinding(control);

        if (control is LineEdit)
            return new ProxyTextInput(control);

        if (control is NTickbox or NCardTypeTickbox or NCardCostTickbox)
            return new ProxyCheckbox(control);

        if (control is NDropdown)
            return new ProxyDropdown(control);

        if (control is NSettingsSlider)
            return new ProxySlider(control);

        if (control is NPaginator)
            return new ProxyPaginator(control);

        if (control is NLabPotionHolder)
            return new ProxyPotionLabHolder(control);

        if (control is NRelicCollectionEntry)
            return new ProxyRelicCollectionEntry(control);

        if (control is NStatEntry)
            return new ProxyStatEntry(control);

        if (control is NRunHistoryPlayerIcon)
            return new ProxyRunHistoryPlayerIcon(control);

        if (control is NMapPointHistoryEntry)
            return new ProxyRunHistoryMapPoint(control);

        if (control is NDeckHistoryEntry)
            return new ProxyDeckHistoryEntry(control);

        if (control is NCardPoolFilter)
            return new ProxyCardPoolFilter(control);

        if (control is NCardViewSortButton)
            return new ProxyCardViewSortButton(control);

        // Top bar elements
        if (control is NTopBarHp or NTopBarGold or NTopBarRoomIcon
            or NTopBarFloorIcon or NTopBarBossIcon)
            return new ProxyTopBar(control);

        // Combat-specific types
        if (control is NOrb)
            return new ProxyOrb(control);

        if (control is NPotionHolder)
            return new ProxyPotionHolder(control);

        if (control is NRelicInventoryHolder or NTreasureRoomRelicHolder or NRelicBasicHolder)
            return new ProxyRelicHolder(control);

        // Reward buttons
        if (control is NRewardButton)
            return new ProxyRewardButton(control);

        // Map points
        if (control is NMapPoint)
            return new ProxyMapPoint(control);

        // Card holder or creature directly focused (e.g., hand cards, creatures via controller nav)
        if (control is NCardHolder)
            return new ProxyCard(control);
        if (control is NCreature)
            return new ProxyCreature(control);

        // Check if this control is a hitbox inside a card holder or creature
        var ancestor = FindAncestor(control);
        if (ancestor != null) return ancestor;

        // Generic NButton and all other NClickableControl subclasses fall through to button
        if (control is NButton)
            return new ProxyButton(control);

        // Fallback for any other focusable control — log so we notice missing proxy types
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[AccessibilityMod] ProxyFactory fallback: {control.GetType().Name} ({control.Name}) resolved as generic ProxyButton");
        return new ProxyButton(control);
    }

    private static ProxyElement? FindAncestor(Control control)
    {
        Node? current = control.GetParent();
        while (current != null)
        {
            if (current is NCardBundle)
                return new ProxyCardBundle(control);
            if (current is NCardHolder)
                return new ProxyCard(control);
            if (current is NCreature)
                return new ProxyCreature(control);
            if (current is MegaCrit.Sts2.Core.Nodes.Multiplayer.NMultiplayerPlayerState playerState)
                return new ProxyMultiplayerPlayerState(control, playerState);
            current = current.GetParent();
        }
        return null;
    }
}
