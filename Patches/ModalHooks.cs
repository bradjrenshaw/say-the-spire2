using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class ModalHooks
{
    public static void Initialize(Harmony harmony)
    {
        var addMethod = AccessTools.Method(typeof(NModalContainer), "Add");
        if (addMethod != null)
        {
            harmony.Patch(addMethod,
                postfix: new HarmonyMethod(typeof(ModalHooks), nameof(AddPostfix)));
            Log.Info("[AccessibilityMod] NModalContainer.Add hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NModalContainer.Add()!");
        }

        var clearMethod = AccessTools.Method(typeof(NModalContainer), "Clear");
        if (clearMethod != null)
        {
            harmony.Patch(clearMethod,
                postfix: new HarmonyMethod(typeof(ModalHooks), nameof(ClearPostfix)));
            Log.Info("[AccessibilityMod] NModalContainer.Clear hook patched.");
        }

        // Patch page turn methods on NCombatRulesFtue to re-announce text
        var toggleLeft = AccessTools.Method(typeof(NCombatRulesFtue), "ToggleLeft");
        var toggleRight = AccessTools.Method(typeof(NCombatRulesFtue), "ToggleRight");
        if (toggleLeft != null)
        {
            harmony.Patch(toggleLeft,
                postfix: new HarmonyMethod(typeof(ModalHooks), nameof(FtuePageTurnPostfix)));
        }
        if (toggleRight != null)
        {
            harmony.Patch(toggleRight,
                postfix: new HarmonyMethod(typeof(ModalHooks), nameof(FtuePageTurnPostfix)));
        }
        if (toggleLeft != null || toggleRight != null)
            Log.Info("[AccessibilityMod] NCombatRulesFtue page turn hooks patched.");
    }

    public static void AddPostfix(Node modalToCreate)
    {
        // Wait one frame for _Ready to run and populate text nodes
        var tree = modalToCreate.GetTree();
        if (tree != null)
        {
            SignalAwaiter awaiter = modalToCreate.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            awaiter.OnCompleted(() => OnModalReady(modalToCreate));
        }
        else
        {
            OnModalReady(modalToCreate);
        }
    }

    public static void ClearPostfix()
    {
        if (ModalScreen.Current != null)
            ScreenManager.RemoveScreen(ModalScreen.Current);
    }

    private static void OnModalReady(Node modal)
    {
        if (!GodotObject.IsInstanceValid(modal)) return;

        AnnounceModal(modal);

        // Push a screen so the modal participates in help and element resolution
        if (ModalScreen.Current != null)
            ScreenManager.RemoveScreen(ModalScreen.Current);
        ScreenManager.PushScreen(new ModalScreen(modal));
    }

    private static void AnnounceModal(Node modal)
    {
        var sb = new StringBuilder();

        // Look for header/title text
        var header = FindTextByNames(modal, new[] { "Header", "Title", "TitleLabel" });
        if (!string.IsNullOrEmpty(header))
            sb.Append(header);

        // Look for body/description text
        var body = FindTextByNames(modal, new[] { "Description", "Body", "BodyLabel" });
        if (!string.IsNullOrEmpty(body))
        {
            if (sb.Length > 0) sb.Append(". ");
            sb.Append(body);
        }

        if (sb.Length > 0)
        {
            var text = sb.ToString();
            Log.Info($"[AccessibilityMod] Modal opened: \"{text}\"");
            SpeechManager.Output(Message.Raw(text));
        }
        else
        {
            Log.Info($"[AccessibilityMod] Modal opened: {modal.GetType().Name} (no readable text found)");
        }
    }

    public static void FtuePageTurnPostfix(NCombatRulesFtue __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance)) return;

        // Wait one frame for text to be updated
        var tree = __instance.GetTree();
        if (tree != null)
        {
            SignalAwaiter awaiter = __instance.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            awaiter.OnCompleted(() => AnnounceModal(__instance));
        }
        else
        {
            AnnounceModal(__instance);
        }
    }

    private static string? FindTextByNames(Node root, string[] names)
    {
        foreach (var name in names)
        {
            var text = FindTextNodeRecursive(root, name);
            if (text != null) return text;
        }
        return null;
    }

    private static string? FindTextNodeRecursive(Node node, string targetName)
    {
        if (node.Name == targetName)
        {
            return ExtractText(node);
        }

        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var result = FindTextNodeRecursive(node.GetChild(i), targetName);
            if (result != null) return result;
        }

        return null;
    }

    private static string? ExtractText(Node node)
    {
        if (node is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return Message.StripBbcode(rtl.Text);
        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;
        return null;
    }
}
