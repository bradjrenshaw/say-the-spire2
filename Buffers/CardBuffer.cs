using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.Views;
namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(HeaderAnnouncement),
    typeof(CostsAnnouncement),
    typeof(DescriptionAnnouncement),
    typeof(EnchantmentAnnouncement),
    typeof(AfflictionAnnouncement),
    typeof(HoverTipsAnnouncement),
    typeof(ExtrasAnnouncement)
)]
public class CardBuffer : Buffer
{
    private CardModel? _model;
    private IReadOnlyList<string> _extraLines = Array.Empty<string>();

    public CardBuffer() : base("card") { }

    public void Bind(CardModel model)
    {
        _model = model;
        _extraLines = Array.Empty<string>();
    }

    public void Bind(CardModel model, IEnumerable<string>? extraLines)
    {
        _model = model;
        _extraLines = extraLines?
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray()
            ?? Array.Empty<string>();
    }

    protected override void ClearBinding()
    {
        _model = null;
        _extraLines = Array.Empty<string>();
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(() => Populate(this, _model, _extraLines));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with card data.
    /// Used by CardBuffer.Update(), UpgradeBuffer, and any other place that
    /// needs to render a CardModel as a buffer. The announcement list and
    /// order both flow through the user's "card" buffer settings, so a
    /// reorder/toggle in mod settings applies everywhere uniformly.
    /// </summary>
    /// <param name="descriptionOverride">
    /// When non-null, used in place of the card's pile description. Lets the
    /// upgrade buffer inject the diff-style upgrade-preview text without
    /// duplicating the rest of the buffer-population logic. Receives the
    /// same bbcode-strip pass as a regular description.
    /// </param>
    public static void Populate(Buffer buffer, CardModel model, IEnumerable<string>? extraLines = null,
        string? descriptionOverride = null)
    {
        var view = CardView.FromModel(model);
        var attrOrder = typeof(CardBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "card", attrOrder, BuildAnnouncements(view, extraLines, descriptionOverride));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(CardView view, IEnumerable<string>? extraLines,
        string? descriptionOverride)
    {
        yield return new HeaderAnnouncement(BuildHeader(view));
        yield return new CostsAnnouncement(BuildCosts(view));

        var desc = string.IsNullOrEmpty(descriptionOverride)
            ? BuildDescription(view)
            : ProxyElement.StripBbcode(descriptionOverride);
        if (!string.IsNullOrEmpty(desc))
            yield return new DescriptionAnnouncement(desc);

        var enchantment = BuildEnchantment(view);
        if (enchantment != null)
            yield return enchantment;

        var affliction = BuildAffliction(view);
        if (affliction != null)
            yield return affliction;

        IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> tips = Array.Empty<MegaCrit.Sts2.Core.HoverTips.IHoverTip>();
        try { tips = view.HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.IHoverTip>().ToList(); }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card hover tips access failed: {e.Message}"); }
        yield return new HoverTipsAnnouncement(tips);

        if (extraLines != null)
            yield return new ExtrasAnnouncement(extraLines);
    }

    private static string BuildHeader(CardView view)
    {
        var typeText = LocalizationManager.GetOrDefault("ui", $"TYPES.{view.Type.ToString().ToUpperInvariant()}", view.Type.ToString());
        var header = $"{view.Title}, {typeText}";
        if (view.Rarity != CardRarity.None)
        {
            var rarityText = LocalizationManager.GetOrDefault("ui", $"RARITIES.{view.Rarity.ToString().ToUpperInvariant()}", view.Rarity.ToString());
            header += $", {rarityText}";
        }
        return header;
    }

    private static string? BuildCosts(CardView view)
    {
        var costs = new List<Message>();
        if (view.EnergyCost != null)
        {
            if (view.EnergyCost.CostsX)
                costs.Add(Message.Localized("ui", "RESOURCE.CARD_X_ENERGY"));
            else
            {
                try { costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = view.EnergyCost.GetWithModifiers(CostModifiers.All) })); }
                catch (Exception e) { Log.Info($"[AccessibilityMod] Energy cost modifier failed: {e.Message}"); costs.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = view.EnergyCost.Canonical })); }
            }
        }
        if (view.HasStarCostX)
            costs.Add(Message.Localized("ui", "RESOURCE.CARD_X_STARS"));
        else if (view.CurrentStarCost >= 0)
            costs.Add(Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = view.StarCostWithModifiers }));
        return costs.Count > 0 ? Message.Join(", ", costs.ToArray()).Resolve() : null;
    }

    private static string? BuildDescription(CardView view)
    {
        try
        {
            var desc = view.DisplayedModel.GetDescriptionForPile(PileType.Hand);
            if (!string.IsNullOrEmpty(desc)) return ProxyElement.StripBbcode(desc);
        }
        catch { /* fall through to None */ }
        try
        {
            var desc = view.DisplayedModel.GetDescriptionForPile(PileType.None);
            if (!string.IsNullOrEmpty(desc)) return ProxyElement.StripBbcode(desc);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card description fallback failed: {e.Message}"); }
        return null;
    }

    private static EnchantmentAnnouncement? BuildEnchantment(CardView view)
    {
        if (view.Enchantment is not { } enchant) return null;
        try
        {
            var title = enchant.Title.GetFormattedText();
            var desc = enchant.DynamicDescription.GetFormattedText();
            int? amount = enchant.ShowAmount ? enchant.DisplayAmount : null;
            bool disabled = enchant.Status == EnchantmentStatus.Disabled;
            return new EnchantmentAnnouncement(title, string.IsNullOrEmpty(desc) ? null : ProxyElement.StripBbcode(desc), amount, disabled);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card enchantment access failed: {e.Message}"); return null; }
    }

    private static AfflictionAnnouncement? BuildAffliction(CardView view)
    {
        if (view.Affliction is not { } affliction) return null;
        try
        {
            var title = affliction.Title.GetFormattedText();
            var desc = affliction.DynamicDescription.GetFormattedText();
            int? amount = affliction.IsStackable ? affliction.Amount : null;
            return new AfflictionAnnouncement(title, string.IsNullOrEmpty(desc) ? null : ProxyElement.StripBbcode(desc), amount);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Card affliction access failed: {e.Message}"); return null; }
    }

    /// <summary>
    /// Compact one-line description of a card hover-tip's referenced card —
    /// "Title, N energy, description". Used when another element (relic, event
    /// option, reward, power) references a card via <see cref="CardHoverTip"/>
    /// and we want that card's info inline in the host's buffer instead of
    /// fanning out to a separate card buffer. The full card buffer review is
    /// reserved for when the user is actually focused on a card.
    /// </summary>
    public static string? FormatHoverTip(CardModel model)
    {
        try
        {
            var view = CardView.FromModel(model);
            var parts = new List<string> { view.Title };

            if (view.EnergyCost != null)
            {
                if (view.EnergyCost.CostsX)
                    parts.Add(Message.Localized("ui", "RESOURCE.CARD_X_ENERGY").Resolve());
                else
                {
                    int cost;
                    try { cost = view.EnergyCost.GetWithModifiers(CostModifiers.All); }
                    catch (Exception e) { Log.Info($"[AccessibilityMod] FormatHoverTip energy modifier failed: {e.Message}"); cost = view.EnergyCost.Canonical; }
                    parts.Add(Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost }).Resolve());
                }
            }
            if (view.HasStarCostX)
                parts.Add(Message.Localized("ui", "RESOURCE.CARD_X_STARS").Resolve());
            else if (view.CurrentStarCost >= 0)
                parts.Add(Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = view.StarCostWithModifiers }).Resolve());

            string? desc = null;
            try { desc = view.DisplayedModel.GetDescriptionForPile(PileType.Hand); }
            catch { try { desc = view.DisplayedModel.GetDescriptionForPile(PileType.None); } catch { } }
            if (!string.IsNullOrEmpty(desc))
                parts.Add(ProxyElement.StripBbcode(desc));

            return string.Join(", ", parts);
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] FormatHoverTip failed: {e.Message}");
            return null;
        }
    }
}
