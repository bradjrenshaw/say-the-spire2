using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class EventHooks
{
    private static readonly FieldInfo? TitleField =
        typeof(NEventLayout).GetField("_title", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? DialogueContainerField =
        typeof(NAncientEventLayout).GetField("_dialogueContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EventField =
        typeof(NEventLayout).GetField("_event", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Initialize(Harmony harmony)
    {
        var setDescription = AccessTools.Method(typeof(NEventLayout), "SetDescription");
        if (setDescription != null)
        {
            harmony.Patch(setDescription,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDescriptionPostfix)));
            Log.Info("[AccessibilityMod] Event SetDescription hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NEventLayout.SetDescription!");
        }

        var initVisuals = AccessTools.Method(typeof(NAncientEventLayout), "InitializeVisuals");
        if (initVisuals != null)
        {
            harmony.Patch(initVisuals,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(AncientInitializeVisualsPostfix)));
            Log.Info("[AccessibilityMod] Ancient InitializeVisuals hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NAncientEventLayout.InitializeVisuals!");
        }

        // Card stealing (SwipePower.Steal)
        var stealMethod = AccessTools.Method(typeof(SwipePower), "Steal");
        if (stealMethod != null)
        {
            harmony.Patch(stealMethod,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardStolenPostfix)));
            Log.Info("[AccessibilityMod] SwipePower.Steal hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find SwipePower.Steal!");
        }

        // Card upgrade
        var upgradeMethod = AccessTools.Method(typeof(CardCmd), "Upgrade",
            new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) });
        if (upgradeMethod != null)
        {
            harmony.Patch(upgradeMethod,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardUpgradePostfix)));
            Log.Info("[AccessibilityMod] CardCmd.Upgrade hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find CardCmd.Upgrade!");
        }

        // Orb events
        var orbChanneled = AccessTools.Method(typeof(Hook), "AfterOrbChanneled");
        if (orbChanneled != null)
        {
            harmony.Patch(orbChanneled,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(OrbChanneledPostfix)));
            Log.Info("[AccessibilityMod] Hook.AfterOrbChanneled hook patched.");
        }

        var orbEvoked = AccessTools.Method(typeof(Hook), "AfterOrbEvoked");
        if (orbEvoked != null)
        {
            harmony.Patch(orbEvoked,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(OrbEvokedPostfix)));
            Log.Info("[AccessibilityMod] Hook.AfterOrbEvoked hook patched.");
        }

        var setDialogueLine = AccessTools.Method(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate");
        if (setDialogueLine != null)
        {
            harmony.Patch(setDialogueLine,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDialogueLinePostfix)));
            Log.Info("[AccessibilityMod] Ancient dialogue line hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NAncientEventLayout.SetDialogueLineAndAnimate!");
        }
    }

    public static void CardUpgradePostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            foreach (var card in cards)
            {
                var name = card.Title;
                if (!string.IsNullOrEmpty(name))
                    EventDispatcher.Enqueue(new CardUpgradeEvent(name));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade hook error: {e.Message}");
        }
    }

    public static void CardStolenPostfix(SwipePower __instance, CardModel card)
    {
        try
        {
            var cardName = card?.Title;
            if (!string.IsNullOrEmpty(cardName))
                CombatScreen.Current?.OnCardStolen(cardName);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card stolen hook error: {e.Message}");
        }
    }

    public static void AncientInitializeVisualsPostfix(NAncientEventLayout __instance)
    {
        try
        {
            var eventModel = EventField?.GetValue(__instance) as AncientEventModel;
            if (eventModel == null) return;

            var title = eventModel.Title?.GetFormattedText();
            var epithet = eventModel.Epithet?.GetFormattedText();

            var text = !string.IsNullOrEmpty(epithet)
                ? $"{title}, {epithet}"
                : title;

            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient event: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient InitializeVisuals hook error: {e.Message}");
        }
    }

    public static void SetDialogueLinePostfix(NAncientEventLayout __instance, int lineIndex)
    {
        try
        {
            var container = DialogueContainerField?.GetValue(__instance) as Node;
            if (container == null) return;

            var child = container.GetChildOrNull<Control>(lineIndex);
            if (child == null) return;

            // NAncientDialogueLine has a %Text (MegaRichTextLabel extends RichTextLabel)
            var textNode = child.GetNodeOrNull<RichTextLabel>("%Text");
            if (textNode == null) return;

            var text = textNode.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient dialogue: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient dialogue hook error: {e.Message}");
        }
    }

    public static void OrbChanneledPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Channeled, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb channeled hook error: {e.Message}");
        }
    }

    public static void OrbEvokedPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Evoked, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb evoked hook error: {e.Message}");
        }
    }

    public static void SetDescriptionPostfix(NEventLayout __instance, string description)
    {
        try
        {
            var title = "";
            var titleLabel = TitleField?.GetValue(__instance);
            if (titleLabel != null)
            {
                var textProp = titleLabel.GetType().GetProperty("Text");
                if (textProp != null)
                    title = textProp.GetValue(titleLabel) as string ?? "";
            }

            if (string.IsNullOrEmpty(description)) return;

            var text = string.IsNullOrEmpty(title) ? description : $"{title}. {description}";
            Log.Info($"[AccessibilityMod] Event description: \"{text}\"");
            SpeechManager.Output(Message.Raw(text));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Event description hook error: {e.Message}");
        }
    }
}
