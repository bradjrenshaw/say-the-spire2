using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Patches;

/// <summary>
/// The game's Sentry crash-reporting GDExtension adds runtime nodes with
/// Godot auto-names like "@SentrySceneTreeWatcher@4" that screen readers
/// announce — and a JAWS dictionary entry can only silence the bare name
/// token, not the @ signs and per-launch counter, which tokenize separately.
///
/// The game itself disables Sentry event reporting for modded sessions
/// (SentryService.DisableSentryIfModded runs before mods initialize), so the
/// watcher is dead weight here: known watcher nodes are freed outright, and
/// any other auto-named Sentry node is renamed to its bare name so the
/// shipped JDF entries match the whole utterance.
/// </summary>
public static class SentryNodeSilencer
{
    private static int _scanned;

    public static void Initialize()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Log.Error("[AccessibilityMod] Sentry silencer: no scene tree available.");
            return;
        }

        RunScan(tree, "initial");
        tree.NodeAdded += OnNodeAdded;

        // The GDExtension can create its nodes after mod initialization, and
        // internal nodes may slip past NodeAdded orderings — rescan late.
        tree.CreateTimer(5.0).Timeout += () => RunScan(tree, "5s");
        tree.CreateTimer(30.0).Timeout += () => RunScan(tree, "30s");

        Log.Info("[AccessibilityMod] Sentry node silencer active.");
    }

    private static void RunScan(SceneTree tree, string label)
    {
        try
        {
            _scanned = 0;
            ScanAndSilence(tree.Root);
            Log.Info($"[AccessibilityMod] Sentry scan ({label}): {_scanned} nodes checked.");
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Sentry scan ({label}) failed: {e.Message}");
        }
    }

    private static void OnNodeAdded(Node node)
    {
        try
        {
            MaybeSilence(node);
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Sentry node silencing failed: {e.Message}");
        }
    }

    private static void ScanAndSilence(Node node)
    {
        _scanned++;
        // Snapshot children (including internal ones — the GDExtension's
        // nodes aren't necessarily regular children) before recursing,
        // because MaybeSilence can free nodes.
        var children = node.GetChildren(includeInternal: true);
        MaybeSilence(node);
        foreach (var child in children)
            ScanAndSilence(child);
    }

    private static void MaybeSilence(Node node)
    {
        string name = node.Name;
        // Auto-named ("@SentryFoo@7") or explicitly named Sentry nodes both
        // qualify; match anywhere in the name to be safe.
        if (!name.Contains("Sentry"))
            return;
        if (name.Length < 2 || name[0] != '@')
        {
            // SentryBootstrap's only job is its _EnterTree call to
            // SentryService.Initialize(), which ran before mods loaded; it
            // has no _Process or _ExitTree and nothing looks it up by path
            // (AfterGameInit is called from NGame). Freeing it removes the
            // last Sentry element screen readers can announce — the JAWS
            // dictionary route proved unreliable for it, likely because
            // window-focus announcements can fire before JAWS switches to
            // the game's app profile.
            if (name == "SentryBootstrap")
            {
                node.QueueFree();
                Log.Info($"[AccessibilityMod] Removed Sentry node \"{name}\".");
                return;
            }

            // Other explicit names are JDF-silenceable; log them so we know
            // what exists.
            Log.Info($"[AccessibilityMod] Sentry node present (explicit name): \"{name}\".");
            return;
        }

        // The tree watcher only exists to capture events the game already
        // refuses to send in modded sessions; drop it entirely.
        if (name.Contains("SentrySceneTreeWatcher"))
        {
            node.QueueFree();
            Log.Info($"[AccessibilityMod] Removed Sentry node \"{name}\".");
            return;
        }

        // Unknown Sentry node: keep it functional but strip the auto-name
        // ("@SentryFoo@7" -> "SentryFoo") so the JDF can silence it. Log so
        // we learn the name and can add a dictionary entry.
        int secondAt = name.IndexOf('@', 1);
        var bareName = secondAt > 1 ? name.Substring(1, secondAt - 1) : name.Trim('@');
        if (bareName.Length == 0)
            return;

        node.Name = bareName;
        if (node is Window window)
            window.Title = " ";
        Log.Info($"[AccessibilityMod] Renamed Sentry node \"{name}\" to \"{node.Name}\".");
    }
}
