using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using SayTheSpire2.Events;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class FocusHooks
{
    private static readonly PropertyInfo IsFocusedProp =
        typeof(NClickableControl).GetProperty("IsFocused", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo? MerchantLabelField =
        typeof(NMerchantDialogue).GetField("_label", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Initialize(Harmony harmony)
    {
        // Patch NClickableControl.RefreshFocus for buttons, tickboxes, dropdowns, etc.
        var refreshFocus = AccessTools.Method(typeof(NClickableControl), "RefreshFocus");
        if (refreshFocus == null)
        {
            Log.Error("[AccessibilityMod] Could not find RefreshFocus method!");
            return;
        }

        var prefix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPrefix), BindingFlags.Static | BindingFlags.Public));
        var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPostfix), BindingFlags.Static | BindingFlags.Public));
        harmony.Patch(refreshFocus, prefix: prefix, postfix: postfix);
        Log.Info("[AccessibilityMod] RefreshFocus hook patched.");

        // Patch NSettingsSlider.OnFocus and NPaginator.OnFocus (not NClickableControl subclasses)
        PatchOnFocus<NSettingsSlider>(harmony, nameof(SettingsControlFocusPostfix), "Slider");
        PatchOnFocus<NPaginator>(harmony, nameof(SettingsControlFocusPostfix), "Paginator");

        // Patch NPaginator.IndexChangeHelper to announce value changes while focused
        var indexChangeHelper = AccessTools.Method(typeof(NPaginator), "IndexChangeHelper");
        if (indexChangeHelper != null)
        {
            harmony.Patch(indexChangeHelper,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(PaginatorIndexChangePostfix)));
            Log.Info("[AccessibilityMod] Paginator IndexChangeHelper hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NPaginator.IndexChangeHelper()!");
        }

        // Patch combat focus: card holders and creatures have their own focus systems
        PatchOnFocus<NHandCardHolder>(harmony, nameof(CardHolderFocusPostfix), "HandCardHolder");
        PatchOnFocus<NGridCardHolder>(harmony, nameof(CardHolderFocusPostfix), "GridCardHolder");

        // Merchant slots have their own focus system (FocusEntered signal, not NClickableControl)
        var merchantOnFocus = AccessTools.Method(typeof(NMerchantSlot), "OnFocus");
        if (merchantOnFocus != null)
        {
            harmony.Patch(merchantOnFocus,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(MerchantSlotFocusPostfix)));
            Log.Info("[AccessibilityMod] MerchantSlot focus hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NMerchantSlot.OnFocus()!");
        }

        // Speech bubbles from creatures (combat dialogue)
        var speechCreateCreature = AccessTools.Method(typeof(NSpeechBubbleVfx), "Create",
            new[] { typeof(string), typeof(Creature), typeof(double), typeof(VfxColor) });
        if (speechCreateCreature != null)
        {
            harmony.Patch(speechCreateCreature,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(SpeechBubbleCreaturePostfix)));
            Log.Info("[AccessibilityMod] SpeechBubble creature hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NSpeechBubbleVfx.Create(string,Creature,...)!");
        }

        // Speech bubbles without a creature (position-based)
        var speechCreatePos = AccessTools.Method(typeof(NSpeechBubbleVfx), "Create",
            new[] { typeof(string), typeof(DialogueSide), typeof(Godot.Vector2), typeof(double), typeof(VfxColor) });
        if (speechCreatePos != null)
        {
            harmony.Patch(speechCreatePos,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(SpeechBubblePostfix)));
            Log.Info("[AccessibilityMod] SpeechBubble position hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NSpeechBubbleVfx.Create(string,DialogueSide,...)!");
        }

        // Merchant dialogue (purchase success/failure, open inventory, etc.)
        var showRandom = AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom");
        if (showRandom != null)
        {
            harmony.Patch(showRandom,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(MerchantDialoguePostfix)));
            Log.Info("[AccessibilityMod] MerchantDialogue hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NMerchantDialogue.ShowRandom()!");
        }

        var creatureOnFocus = AccessTools.Method(typeof(NCreature), "OnFocus");
        if (creatureOnFocus != null)
        {
            harmony.Patch(creatureOnFocus,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(CreatureFocusPostfix)));
            Log.Info("[AccessibilityMod] Creature focus hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NCreature.OnFocus()!");
        }
    }

    public static void RefreshFocusPrefix(NClickableControl __instance, out bool __state)
    {
        __state = (bool)IsFocusedProp.GetValue(__instance)!;
    }

    public static void RefreshFocusPostfix(NClickableControl __instance, bool __state)
    {
        bool nowFocused = (bool)IsFocusedProp.GetValue(__instance)!;
        if (nowFocused && !__state)
        {
            UIManager.QueueFocus(__instance);
        }
    }

    public static void SettingsControlFocusPostfix(Control __instance)
    {
        UIManager.QueueFocus(__instance);
    }

    public static void PaginatorIndexChangePostfix(NPaginator __instance)
    {
        var element = ScreenManager.ResolveElement(__instance);
        if (element is { IsFocused: true })
        {
            var status = element.GetStatusString();
            if (!string.IsNullOrEmpty(status))
                SpeechManager.Output(status);
        }
    }

    public static void CardHolderFocusPostfix(NCardHolder __instance)
    {
        UIManager.QueueFocus(__instance, new ProxyCard(__instance));
    }

    public static void SpeechBubblePostfix(string text)
    {
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                var clean = ProxyElement.StripBbcode(text);
                if (!string.IsNullOrEmpty(clean))
                    EventDispatcher.Enqueue(new DialogueEvent(null, clean));
            }
        }
        catch { }
    }

    public static void SpeechBubbleCreaturePostfix(string text, Creature speaker)
    {
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                var clean = ProxyElement.StripBbcode(text);
                if (!string.IsNullOrEmpty(clean))
                    EventDispatcher.Enqueue(new DialogueEvent(speaker.Name, clean));
            }
        }
        catch { }
    }

    public static void MerchantDialoguePostfix(NMerchantDialogue __instance)
    {
        try
        {
            var label = MerchantLabelField?.GetValue(__instance) as Godot.RichTextLabel;
            if (label == null) return;
            var text = label.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var clean = ProxyElement.StripBbcode(text);
                if (!string.IsNullOrEmpty(clean))
                    EventDispatcher.Enqueue(new DialogueEvent("Merchant", clean));
            }
        }
        catch { }
    }

    public static void MerchantSlotFocusPostfix(NMerchantSlot __instance)
    {
        UIManager.QueueFocus(__instance, new ProxyMerchantSlot(__instance));
    }

    public static void CreatureFocusPostfix(NCreature __instance)
    {
        UIManager.QueueFocus(__instance, new ProxyCreature(__instance));
    }

    private static void PatchOnFocus<T>(Harmony harmony, string postfixMethodName, string label)
    {
        var onFocus = AccessTools.Method(typeof(T), "OnFocus");
        if (onFocus != null)
        {
            var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.Public));
            harmony.Patch(onFocus, postfix: postfix);
            Log.Info($"[AccessibilityMod] {label} focus hook patched.");
        }
        else
        {
            Log.Error($"[AccessibilityMod] Could not find {typeof(T).Name}.OnFocus()!");
        }
    }
}
