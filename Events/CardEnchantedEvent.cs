using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_enchanted", "Card Enchanted", category: "Cards")]
public class CardEnchantedEvent : GameEvent
{
    private const string BasePath = "events.card_enchanted";

    private readonly string _cardName;
    private readonly string _enchantmentName;
    private readonly int? _amount;

    /// <param name="amount">
    /// The enchantment's display amount, or null when the game's VFX hides
    /// the amount label (i.e. <c>EnchantmentModel.ShowAmount == false</c>).
    /// We only surface the number when the visual does.
    /// </param>
    public CardEnchantedEvent(string cardName, string enchantmentName, int? amount)
    {
        _cardName = cardName;
        _enchantmentName = enchantmentName;
        _amount = amount;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("show_amount", "Show Amount", true,
            localizationKey: "EVENTS.CARD_ENCHANTED.SHOW_AMOUNT"));
    }

    public override Message? GetMessage()
    {
        if (_amount.HasValue && ModSettings.GetValue<bool>($"{BasePath}.show_amount"))
            return Message.Localized("ui", "EVENT.CARD_ENCHANTED_WITH_AMOUNT",
                new { card = _cardName, enchantment = _enchantmentName, amount = _amount.Value });

        return Message.Localized("ui", "EVENT.CARD_ENCHANTED",
            new { card = _cardName, enchantment = _enchantmentName });
    }
}
