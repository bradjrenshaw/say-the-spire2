using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Audio;

/// <summary>
/// Plays the mod's own sound effects through a Godot AudioStreamPlayer (the
/// game's SFX go through FMOD, which we can't feed raw wavs). Streams are
/// packed as raw wav bytes in the mod PCK and decoded via
/// AudioStreamWav.LoadFromBuffer.
/// </summary>
public static class SoundEffects
{
    private const string WrapSoundPath = "res://SayTheSpire2/audio/wrap.wav";
    /// <summary>Centers closer than this along the nav axis don't count as movement.</summary>
    private const float WrapEpsilon = 4f;

    /// <summary>Set by ModEntry when settings register.</summary>
    public static BoolSetting? WrapSoundSetting { get; set; }

    /// <summary>Master volume for all mod sounds, 0–100. Set by ModEntry.</summary>
    public static IntSetting? VolumeSetting { get; set; }

    private static AudioStreamPlayer? _player;
    private static AudioStream? _wrapStream;
    private static bool _loadFailed;

    /// <summary>
    /// Plays the wrap sound when a focus move wrapped around — e.g. right
    /// from the rightmost hand card landing on the leftmost.
    ///
    /// Elements registered in the mod's container model are checked
    /// structurally: a wrap is last→first or first→last within the SAME
    /// container, and any move involving a different container (group swaps
    /// like bestiary sidebar → detail panel) is silent — screen positions in
    /// mod-wired screens are logical, not spatial, so geometry lies there.
    /// Containerless elements (fallback proxies like hand cards) keep the
    /// geometric heuristic: focus moved against the pressed nav direction.
    /// </summary>
    public static void CheckWrap(UI.Elements.UIElement? fromElement, UI.Elements.UIElement? toElement,
        Control? from, Control? to)
    {
        try
        {
            if (WrapSoundSetting is { Value: false })
                return;
            if (from == null || to == null || from == to)
                return;
            if (!GodotObject.IsInstanceValid(from) || !GodotObject.IsInstanceValid(to))
                return;

            var fromParent = fromElement?.Parent;
            var toParent = toElement?.Parent;
            if (fromParent != null || toParent != null)
            {
                if (fromParent == null || !ReferenceEquals(fromParent, toParent))
                    return;

                // Direction matters: in a two-item list every move is also a
                // first↔last move, and Home/End jumps land on the ends
                // without wrapping. Only a forward move (down/right) off the
                // last item or a backward move (up/left) off the first is a
                // real wrap.
                bool forward = IsPressedUnambiguously("ui_down", "ui_up") || IsPressedUnambiguously("ui_right", "ui_left");
                bool backward = IsPressedUnambiguously("ui_up", "ui_down") || IsPressedUnambiguously("ui_left", "ui_right");

                var count = fromParent.Children.Count;
                var fromIndex = fromParent.IndexOf(fromElement!);
                var toIndex = fromParent.IndexOf(toElement!);
                if (count >= 2 && fromIndex >= 0 && toIndex >= 0
                    && ((forward && fromIndex == count - 1 && toIndex == 0)
                        || (backward && fromIndex == 0 && toIndex == count - 1)))
                {
                    PlayWrap();
                }
                return;
            }

            // Map nodes are scattered graph points, not a row or column — a
            // "right" neighbor can sit geometrically left or above, so the
            // spatial heuristic misfires there. No wrap sound on the map.
            if (from is MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint
                || to is MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint)
                return;

            var oldRect = from.GetGlobalRect();
            var newRect = to.GetGlobalRect();

            // Mod-created focus anchors are zero-size nodes at the scene
            // origin — their positions say nothing about travel direction.
            if (oldRect.Size.X < 2f || oldRect.Size.Y < 2f
                || newRect.Size.X < 2f || newRect.Size.Y < 2f)
                return;

            var oldCenter = oldRect.GetCenter();
            var newCenter = newRect.GetCenter();

            bool wrapped =
                (IsPressedUnambiguously("ui_right", "ui_left") && newCenter.X < oldCenter.X - WrapEpsilon)
                || (IsPressedUnambiguously("ui_left", "ui_right") && newCenter.X > oldCenter.X + WrapEpsilon)
                || (IsPressedUnambiguously("ui_down", "ui_up") && newCenter.Y < oldCenter.Y - WrapEpsilon)
                || (IsPressedUnambiguously("ui_up", "ui_down") && newCenter.Y > oldCenter.Y + WrapEpsilon);

            if (wrapped)
                PlayWrap();
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Wrap sound check failed: {e.Message}");
        }
    }

    /// <summary>
    /// True when <paramref name="action"/> is pressed and its opposite is
    /// not. Rapid direction alternation leaves both directions pressed at
    /// announce time (the focus change is processed a frame after the
    /// input), and guessing the travel direction then produces false wraps.
    /// </summary>
    private static bool IsPressedUnambiguously(string action, string opposite)
    {
        return Godot.Input.IsActionPressed(action) && !Godot.Input.IsActionPressed(opposite);
    }

    public static void PlayWrap()
    {
        try
        {
            if (_loadFailed) return;
            EnsurePlayer();
            if (_player == null || !GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree())
                return;
            var volume = VolumeSetting?.Get() ?? 100;
            _player.VolumeDb = Mathf.LinearToDb(volume / 100f);
            _player.Play();
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Wrap sound playback failed: {e.Message}");
        }
    }

    private static void EnsurePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            return;

        if (_wrapStream == null)
        {
            var bytes = FileAccess.GetFileAsBytes(WrapSoundPath);
            if (bytes == null || bytes.Length == 0)
            {
                _loadFailed = true;
                Log.Error($"[AccessibilityMod] Could not read {WrapSoundPath} from the mod PCK.");
                return;
            }
            _wrapStream = AudioStreamWav.LoadFromBuffer(bytes);
            if (_wrapStream == null)
            {
                _loadFailed = true;
                Log.Error("[AccessibilityMod] Failed to decode wrap.wav.");
                return;
            }
        }

        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        _player = new AudioStreamPlayer
        {
            Name = "AccessibilitySoundPlayer",
            Stream = _wrapStream,
        };
        tree.Root.AddChild(_player);
    }
}
