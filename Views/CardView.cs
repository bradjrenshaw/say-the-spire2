using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over a game CardModel, optionally bound to a card holder. Owns the
/// reflection and parent-walking logic for card lookups. For grid holders the
/// displayed model may differ from the base model (upgrade preview).
/// </summary>
public class CardView
{
    /// <summary>The model currently being shown (grid holders can show an upgrade preview).</summary>
    public CardModel DisplayedModel { get; }

    /// <summary>The underlying model — what an upgrade buffer reads.</summary>
    public CardModel BaseModel { get; }

    /// <summary>The card's current holder, if constructed via a Control. Null for model-only views.</summary>
    public NCardHolder? Holder { get; }

    private CardView(CardModel displayed, CardModel baseModel, NCardHolder? holder)
    {
        DisplayedModel = displayed;
        BaseModel = baseModel;
        Holder = holder;
    }

    /// <summary>
    /// Walks up from a Godot control to find the enclosing NCardHolder and resolves
    /// the displayed and base models. Null if no holder ancestor or the holder has
    /// no card model.
    /// </summary>
    public static CardView? FromControl(Control? control)
    {
        if (control == null) return null;
        var holder = FindAncestor<NCardHolder>(control);
        if (holder == null) return null;

        var baseModel = holder.CardModel;
        if (baseModel == null) return null;

        var displayed = holder is NGridCardHolder
            ? (holder.CardNode?.Model ?? baseModel)
            : baseModel;

        return new CardView(displayed, baseModel, holder);
    }

    public static CardView FromModel(CardModel model) => new(model, model, null);

    public string Title => DisplayedModel.Title;
    public CardType Type => DisplayedModel.Type;

    /// <summary>Stable key suitable for localization/type announcements ("attack", "skill", ...).</summary>
    public string TypeKey => Type.ToString().ToLowerInvariant();

    public string? EnchantmentTitle => DisplayedModel.Enchantment?.Title?.GetFormattedText();
    public string? AfflictionTitle => DisplayedModel.Affliction?.Title?.GetFormattedText();
    public int ReplayCount => DisplayedModel.BaseReplayCount;

    public CardEnergyCost? EnergyCost => DisplayedModel.EnergyCost;
    public bool HasStarCostX => DisplayedModel.HasStarCostX;
    public int CurrentStarCost => DisplayedModel.CurrentStarCost;

    /// <summary>
    /// Star cost including modifiers (Void Form, etc). Falls back to CurrentStarCost
    /// and logs if the modifier calculation throws — the fallback is meaningful.
    /// </summary>
    public int StarCostWithModifiers
    {
        get
        {
            try { return DisplayedModel.GetStarCostWithModifiers(); }
            catch (Exception e)
            {
                Log.Info($"[AccessibilityMod] GetStarCostWithModifiers failed: {e.Message}");
                return DisplayedModel.CurrentStarCost;
            }
        }
    }

    /// <summary>
    /// The card's pile description, bbcode-stripped. Null if unavailable or if the
    /// model throws while building the description.
    /// </summary>
    public string? Description
    {
        get
        {
            try
            {
                var desc = DisplayedModel.GetDescriptionForPile(PileType.None);
                return string.IsNullOrEmpty(desc) ? null : Message.StripBbcode(desc);
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Card tooltip description failed: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Bbcode-stripped library stats text (only meaningful when the card is in a
    /// visible grid-holder library stats panel). Null otherwise.
    /// </summary>
    public string? LibraryStatsText
    {
        get
        {
            if (Holder is not NGridCardHolder grid) return null;
            var stats = grid.CardLibraryStats;
            if (stats == null || !stats.Visible) return null;
            var label = stats.GetNodeOrNull<MegaRichTextLabel>("%Label");
            var text = label?.Text;
            return string.IsNullOrWhiteSpace(text) ? null : Message.StripBbcode(text);
        }
    }

    public bool IsShowingUpgradedCard =>
        Holder is NGridCardHolder grid && grid.IsShowingUpgradedCard;

    private static T? FindAncestor<T>(Node? node) where T : class
    {
        var current = node;
        while (current != null)
        {
            if (current is T match) return match;
            current = current.GetParent();
        }
        return null;
    }
}
