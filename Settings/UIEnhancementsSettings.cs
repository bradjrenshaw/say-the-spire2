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
    public static BoolSetting Combat { get; private set; } = null!;
    public static BoolSetting HandSelect { get; private set; } = null!;
    public static BoolSetting CrystalSphere { get; private set; } = null!;
    public static BoolSetting RestSite { get; private set; } = null!;
    public static BoolSetting CharacterSelect { get; private set; } = null!;
    public static BoolSetting CustomRun { get; private set; } = null!;
    public static BoolSetting CustomRunLoad { get; private set; } = null!;
    public static BoolSetting DailyRun { get; private set; } = null!;
    public static BoolSetting DailyRunLoad { get; private set; } = null!;
    public static BoolSetting CardLibrary { get; private set; } = null!;
    public static BoolSetting Compendium { get; private set; } = null!;
    public static BoolSetting RunHistory { get; private set; } = null!;
    public static BoolSetting Stats { get; private set; } = null!;
    public static BoolSetting Timeline { get; private set; } = null!;
    public static BoolSetting RunTopBar { get; private set; } = null!;
    public static BoolSetting GameOver { get; private set; } = null!;
    public static BoolSetting MerchantTargeting { get; private set; } = null!;
    public static BoolSetting BundlePreview { get; private set; } = null!;

    public static void Register(CategorySetting parent)
    {
        var category = new CategorySetting(
            "ui_enhancements",
            LocalizationManager.GetOrDefault("ui", "UI_ENHANCEMENTS.CATEGORY", "UI Enhancements"));
        parent.Add(category);

        Combat = AddToggle(category,
            "combat", "UI_ENHANCEMENTS.COMBAT", "Combat Screen");
        HandSelect = AddToggle(category,
            "hand_select", "UI_ENHANCEMENTS.HAND_SELECT", "Hand Select Screen");
        CrystalSphere = AddToggle(category,
            "crystal_sphere", "UI_ENHANCEMENTS.CRYSTAL_SPHERE", "Crystal Sphere");
        RestSite = AddToggle(category,
            "rest_site", "UI_ENHANCEMENTS.REST_SITE", "Rest Site");
        CharacterSelect = AddToggle(category,
            "character_select", "UI_ENHANCEMENTS.CHARACTER_SELECT", "Character Select");
        CustomRun = AddToggle(category,
            "custom_run", "UI_ENHANCEMENTS.CUSTOM_RUN", "Custom Run Setup");
        CustomRunLoad = AddToggle(category,
            "custom_run_load", "UI_ENHANCEMENTS.CUSTOM_RUN_LOAD", "Custom Run Load");
        DailyRun = AddToggle(category,
            "daily_run", "UI_ENHANCEMENTS.DAILY_RUN", "Daily Run Setup");
        DailyRunLoad = AddToggle(category,
            "daily_run_load", "UI_ENHANCEMENTS.DAILY_RUN_LOAD", "Daily Run Load");
        CardReward = AddToggle(category,
            "card_reward", "UI_ENHANCEMENTS.CARD_REWARD", "Card Reward Screen");
        Rewards = AddToggle(category,
            "rewards", "UI_ENHANCEMENTS.REWARDS", "Rewards Screen");
        CardLibrary = AddToggle(category,
            "card_library", "UI_ENHANCEMENTS.CARD_LIBRARY", "Card Library");
        Compendium = AddToggle(category,
            "compendium", "UI_ENHANCEMENTS.COMPENDIUM", "Compendium Menu");
        RunHistory = AddToggle(category,
            "run_history", "UI_ENHANCEMENTS.RUN_HISTORY", "Run History");
        Stats = AddToggle(category,
            "stats", "UI_ENHANCEMENTS.STATS", "Stats Screen");
        Timeline = AddToggle(category,
            "timeline", "UI_ENHANCEMENTS.TIMELINE", "Timeline");
        RunTopBar = AddToggle(category,
            "run_top_bar", "UI_ENHANCEMENTS.RUN_TOP_BAR", "Run Top Bar");
        GameOver = AddToggle(category,
            "game_over", "UI_ENHANCEMENTS.GAME_OVER", "Game Over Screen");
        MerchantTargeting = AddToggle(category,
            "merchant_targeting", "UI_ENHANCEMENTS.MERCHANT_TARGETING", "Merchant Targeting Auto-focus");
        BundlePreview = AddToggle(category,
            "bundle_preview", "UI_ENHANCEMENTS.BUNDLE_PREVIEW", "Card Bundle Preview");
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
