using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class GameOverScreen : GameScreen
{
    public static GameOverScreen? Current { get; private set; }

    public override string? ScreenName => null; // Announced via banner instead

    private static readonly FieldInfo? ScoreField =
        typeof(NGameOverScreen).GetField("_score", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EncounterQuoteField =
        typeof(NGameOverScreen).GetField("_encounterQuote", BindingFlags.Instance | BindingFlags.NonPublic);

    protected override void BuildRegistry()
    {
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public void OnBannerAndQuote(NGameOverScreen instance)
    {
        try
        {
            var banner = instance.GetNodeOrNull("%Banner");
            var quoteLabel = instance.GetNodeOrNull("%DeathQuoteLabel");

            string? title = null;
            if (banner != null)
                title = ProxyElement.FindChildTextPublic(banner);

            // Read the encounter-specific quote (e.g., "The Ironclad had simply given up.")
            // This is set during InitializeBannerAndQuote but only displayed later via animation.
            string? quote = EncounterQuoteField?.GetValue(instance) as string;
            if (string.IsNullOrEmpty(quote) && quoteLabel is RichTextLabel rtl)
                quote = Message.StripBbcode(rtl.Text);

            var message = "";
            if (!string.IsNullOrEmpty(title))
                message = title;
            if (!string.IsNullOrEmpty(quote))
                message += string.IsNullOrEmpty(message) ? quote : $". {quote}";

            if (!string.IsNullOrEmpty(message))
            {
                Log.Info($"[AccessibilityMod] Game over: {message}");
                SpeechManager.Output(Message.Raw(message));

                var uiBuffer = BufferManager.Instance.GetBuffer("ui");
                if (uiBuffer != null)
                {
                    uiBuffer.Clear();
                    uiBuffer.Add(message);
                    BufferManager.Instance.EnableBuffer("ui", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver banner error: {ex.Message}");
        }
    }

    public void OnBadge(string locEntryKey, string? locAmountKey, int amount)
    {
        try
        {
            var locString = new LocString("game_over_screen", locEntryKey);
            if (locAmountKey != null)
                locString.Add(locAmountKey, amount);
            var text = locString.GetFormattedText();
            if (string.IsNullOrEmpty(text)) return;

            var stripped = Message.StripBbcode(text);
            Log.Info($"[AccessibilityMod] Badge: {stripped}");
            SpeechManager.Output(Message.Raw(stripped));

            var uiBuffer = BufferManager.Instance.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Add(stripped);
                BufferManager.Instance.EnableBuffer("ui", true);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Badge error: {ex.Message}");
        }
    }

    public void OnScore(NGameOverScreen instance)
    {
        try
        {
            var score = ScoreField?.GetValue(instance);
            if (score is int scoreVal)
            {
                var message = $"Score: {scoreVal}";
                Log.Info($"[AccessibilityMod] {message}");
                SpeechManager.Output(Message.Raw(message));

                var uiBuffer = BufferManager.Instance.GetBuffer("ui");
                if (uiBuffer != null)
                {
                    uiBuffer.Add(message);
                    BufferManager.Instance.EnableBuffer("ui", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Score error: {ex.Message}");
        }
    }
}
