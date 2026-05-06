using SayTheSpire2.Localization;

namespace SayTheSpire2.Settings;

/// <summary>
/// Per-screen toggles for the focus-neighbor rewiring and related focus
/// management the mod layers on top of the game's defaults. Each toggle
/// defaults to enabled. Disabling a toggle stops the mod from maintaining
/// its wiring on that screen — it does NOT actively restore the game's
/// defaults, so disabling a screen whose rewiring fills a real navigability
/// gap may leave that screen unnavigable until re-enabled.
/// </summary>
public static class UIEnhancementsSettings
{
    public static BoolSetting CardReward { get; private set; } = null!;
    public static BoolSetting Rewards { get; private set; } = null!;

    public static void Register(CategorySetting parent)
    {
        var category = new CategorySetting(
            "ui_enhancements",
            LocalizationManager.GetOrDefault("ui", "UI_ENHANCEMENTS.CATEGORY", "UI Enhancements"));
        parent.Add(category);

        CardReward = AddToggle(category,
            "card_reward", "UI_ENHANCEMENTS.CARD_REWARD", "Card Reward Screen");
        Rewards = AddToggle(category,
            "rewards", "UI_ENHANCEMENTS.REWARDS", "Rewards Screen");
    }

    private static BoolSetting AddToggle(CategorySetting parent, string key, string locKey, string fallback)
    {
        var setting = new BoolSetting(
            key,
            LocalizationManager.GetOrDefault("ui", locKey, fallback),
            defaultValue: true);
        parent.Add(setting);
        return setting;
    }
}
