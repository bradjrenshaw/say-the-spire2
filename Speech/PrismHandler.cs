using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Speech;

/// <summary>
/// Speech handler routing through Prism (https://github.com/ethindp/prism).
/// Prism is a unified abstraction over screen-reader and TTS backends (NVDA,
/// JAWS, SAPI, OneCore, etc.). Positioned first in the handler chain — Tolk
/// remains as a fallback while users migrate.
/// </summary>
public class PrismHandler : ISpeechHandler
{
    private const string AutoBackend = "auto";

    private IntPtr _ctx = IntPtr.Zero;
    private IntPtr _backend = IntPtr.Zero;
    private string? _activeBackendName;
    private PrismNative.BackendFeatures _backendFeatures;
    private CategorySetting? _settings;
    private ChoiceSetting? _backendSetting;
    private IntSetting? _rateSetting;
    private ChoiceSetting? _voiceSetting;

    public string Key => "prism";
    public string Label => "Prism";

    public CategorySetting? GetSettings()
    {
        if (_settings != null) return _settings;

        _settings = new CategorySetting(Key, Label);

        var choices = new List<Choice> { new Choice(AutoBackend, "Auto (Best Available)") };
        var voiceChoices = new List<Choice>();
        // Features of the AVSpeech backend specifically, used below to decide
        // whether the Rate/Voice settings are worth showing at all. Left at
        // 0 (and both settings skipped) on platforms/registries where
        // AVSpeech never shows up, e.g. Windows.
        var avSpeechFeatures = (PrismNative.BackendFeatures)0;
        // Enumerate the registry and keep only backends whose engine is
        // actually available on this machine. prism_backend_get_features may
        // be called pre-initialize; the SupportedAtRuntime bit is advisory
        // (init may still fail) but it filters out the obviously-irrelevant
        // entries (e.g. JAWS on a system without JAWS installed).
        var probeCtx = PrismNative.Init(IntPtr.Zero);
        if (probeCtx != IntPtr.Zero)
        {
            try
            {
                var count = (int)PrismNative.RegistryCount(probeCtx).ToUInt64();
                for (int i = 0; i < count; i++)
                {
                    var id = PrismNative.RegistryIdAt(probeCtx, (UIntPtr)(uint)i);
                    var name = PrismNative.RegistryName(probeCtx, id);
                    if (string.IsNullOrEmpty(name)) continue;

                    var backend = PrismNative.RegistryCreate(probeCtx, id);
                    if (backend == IntPtr.Zero) continue;
                    try
                    {
                        var features = (PrismNative.BackendFeatures)PrismNative.BackendGetFeatures(backend);
                        if ((features & PrismNative.BackendFeatures.SupportedAtRuntime) != 0)
                            choices.Add(new Choice(name, name));

                        // AVSpeech (macOS) is the only backend where Prism
                        // exposes direct voice/rate control the way SAPI does
                        // on Windows — other backends (NVDA, JAWS, VoiceOver)
                        // relay to a screen reader that owns those settings
                        // itself. Probe its voice list now so it's ready
                        // regardless of which backend ends up selected.
                        if (id == PrismNative.AvSpeechBackendId)
                        {
                            avSpeechFeatures = features;
                            voiceChoices.AddRange(ProbeVoices(backend, features));
                        }
                    }
                    finally { PrismNative.BackendFree(backend); }
                }
            }
            finally { PrismNative.Shutdown(probeCtx); }
        }

        _backendSetting = new ChoiceSetting("backend", "Backend", AutoBackend, choices, localizationKey: "SPEECH.PRISM.BACKEND");
        _settings.Add(_backendSetting);
        _backendSetting.Changed += _ => RebindBackend();

        // Only add these when AVSpeech is actually present and advertises the
        // relevant capability — otherwise (e.g. on Windows, where AVSpeech
        // never appears in the registry) they'd be dead controls: a Rate
        // slider that never applies, or a Voice picker with nothing in it.
        if ((avSpeechFeatures & PrismNative.BackendFeatures.SupportsSetRate) != 0)
        {
            _rateSetting = new IntSetting("rate", "Rate", defaultValue: 50, min: 0, max: 100, step: 5, localizationKey: "SPEECH.PRISM.RATE");
            _settings.Add(_rateSetting);
            _rateSetting.Changed += v =>
            {
                if (_backend != IntPtr.Zero && (_backendFeatures & PrismNative.BackendFeatures.SupportsSetRate) != 0)
                    PrismNative.BackendSetRate(_backend, v / 100f);
            };
        }

        if (voiceChoices.Count > 0)
        {
            _voiceSetting = new ChoiceSetting("voice", "Voice", voiceChoices[0].Key, voiceChoices, localizationKey: "SPEECH.PRISM.VOICE");
            _settings.Add(_voiceSetting);
            _voiceSetting.Changed += v =>
            {
                if (uint.TryParse(v, out var id))
                    ApplyVoiceById(id);
            };
        }

        return _settings;
    }

    /// <summary>
    /// Enumerates the voice list of a temporarily-probed backend (used at
    /// settings-build time, before any backend is actually acquired for
    /// speech). Requires initializing the probe backend since voice
    /// enumeration on AVSpeech only works post-init.
    ///
    /// Choice.Key is the voice's numeric index (as a string), not its name —
    /// AVSpeech voice lists can contain multiple voices sharing the same
    /// display name (e.g. two "Reed" entries for different variants/regions),
    /// so the name alone isn't a unique, stable selector. The language is
    /// appended to the label so same-named entries stay distinguishable in
    /// the menu.
    /// </summary>
    private static List<Choice> ProbeVoices(IntPtr backend, PrismNative.BackendFeatures features)
    {
        var result = new List<Choice>();
        if ((features & PrismNative.BackendFeatures.SupportsGetVoiceName) == 0) return result;

        var initErr = PrismNative.BackendInitialize(backend);
        if (initErr != PrismNative.PrismError.Ok && initErr != PrismNative.PrismError.AlreadyInitialized)
            return result;

        if (PrismNative.BackendRefreshVoices(backend) != PrismNative.PrismError.Ok) return result;
        if (PrismNative.BackendCountVoices(backend, out var count) != PrismNative.PrismError.Ok) return result;

        var hasLanguage = (features & PrismNative.BackendFeatures.SupportsGetVoiceLanguage) != 0;
        for (uint i = 0; i < count.ToUInt32(); i++)
        {
            var name = PrismNative.BackendGetVoiceName(backend, (UIntPtr)i);
            if (string.IsNullOrEmpty(name)) continue;

            var language = hasLanguage ? PrismNative.BackendGetVoiceLanguage(backend, (UIntPtr)i) : null;
            var label = string.IsNullOrEmpty(language) ? name : $"{name} ({language})";
            result.Add(new Choice(i.ToString(), label));
        }
        return result;
    }

    public bool Detect()
    {
        // Probing the runtime requires loading prism.dll, which is exactly
        // what Load does. Detect just checks whether Load would succeed.
        try
        {
            var ctx = PrismNative.Init(IntPtr.Zero);
            if (ctx == IntPtr.Zero) return false;
            try
            {
                var backend = PrismNative.RegistryCreateBest(ctx);
                if (backend == IntPtr.Zero) return false;
                PrismNative.BackendFree(backend);
                return true;
            }
            finally { PrismNative.Shutdown(ctx); }
        }
        catch (DllNotFoundException) { return false; }
        catch (Exception ex)
        {
            Log.Info($"[AccessibilityMod] PrismHandler.Detect failed: {ex.Message}");
            return false;
        }
    }

    public bool Load()
    {
        try
        {
            _ctx = PrismNative.Init(IntPtr.Zero);
            if (_ctx == IntPtr.Zero)
            {
                Log.Error("[AccessibilityMod] PrismHandler: prism_init returned NULL.");
                return false;
            }

            return AcquireBackend();
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] PrismHandler failed to load: {ex}");
            Unload();
            return false;
        }
    }

    public void Unload()
    {
        if (_backend != IntPtr.Zero)
        {
            try { PrismNative.BackendStop(_backend); }
            catch (Exception ex) { Log.Info($"[AccessibilityMod] PrismHandler stop on unload failed: {ex.Message}"); }
            try { PrismNative.BackendFree(_backend); }
            catch (Exception ex) { Log.Info($"[AccessibilityMod] PrismHandler free on unload failed: {ex.Message}"); }
            _backend = IntPtr.Zero;
        }
        if (_ctx != IntPtr.Zero)
        {
            try { PrismNative.Shutdown(_ctx); }
            catch (Exception ex) { Log.Info($"[AccessibilityMod] PrismHandler shutdown failed: {ex.Message}"); }
            _ctx = IntPtr.Zero;
        }
        _activeBackendName = null;
        _backendFeatures = 0;
    }

    public bool Speak(string text, bool interrupt = false)
    {
        if (_backend == IntPtr.Zero) return false;
        try
        {
            var err = PrismNative.BackendSpeak(_backend, text, interrupt);
            return err == PrismNative.PrismError.Ok;
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] PrismHandler.Speak failed: {ex.Message}");
            return false;
        }
    }

    public bool Output(string text, bool interrupt = false)
    {
        if (_backend == IntPtr.Zero) return false;
        try
        {
            // prism_backend_output drives both speech and braille when supported.
            // For backends that don't support it (e.g., raw SAPI), fall through
            // to plain speak so we still produce audio. The feature bitmask
            // was cached at backend init — querying it per call is what was
            // costing us an extra NVDA RPC round-trip on every Output.
            if ((_backendFeatures & PrismNative.BackendFeatures.SupportsOutput) != 0)
            {
                var err = PrismNative.BackendOutput(_backend, text, interrupt);
                if (err == PrismNative.PrismError.Ok) return true;
                if (err != PrismNative.PrismError.NotImplemented)
                    Log.Info($"[AccessibilityMod] PrismHandler.Output → {err}, falling back to Speak.");
            }
            return Speak(text, interrupt);
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] PrismHandler.Output failed: {ex.Message}");
            return false;
        }
    }

    public bool Silence()
    {
        if (_backend == IntPtr.Zero) return false;
        try
        {
            var err = PrismNative.BackendStop(_backend);
            return err == PrismNative.PrismError.Ok;
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] PrismHandler.Silence failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Acquire a backend based on the user's preference (auto = highest-priority
    /// that initializes; or a specific backend by name from the registry).
    /// </summary>
    private bool AcquireBackend()
    {
        if (_ctx == IntPtr.Zero) return false;
        var preferred = _backendSetting?.Get() ?? AutoBackend;

        if (preferred == AutoBackend)
        {
            _backend = PrismNative.RegistryCreateBest(_ctx);
        }
        else
        {
            var count = (int)PrismNative.RegistryCount(_ctx).ToUInt64();
            ulong id = 0;
            for (int i = 0; i < count; i++)
            {
                var candidate = PrismNative.RegistryIdAt(_ctx, (UIntPtr)(uint)i);
                if (PrismNative.RegistryName(_ctx, candidate) == preferred)
                {
                    id = candidate;
                    break;
                }
            }
            if (id == 0)
            {
                Log.Error($"[AccessibilityMod] PrismHandler: backend '{preferred}' not in registry. Falling back to auto.");
                _backend = PrismNative.RegistryCreateBest(_ctx);
            }
            else
            {
                _backend = PrismNative.RegistryCreate(_ctx, id);
                if (_backend != IntPtr.Zero)
                {
                    var initErr = PrismNative.BackendInitialize(_backend);
                    if (initErr != PrismNative.PrismError.Ok && initErr != PrismNative.PrismError.AlreadyInitialized)
                    {
                        Log.Error($"[AccessibilityMod] PrismHandler: backend '{preferred}' init failed ({initErr}). Falling back to auto.");
                        PrismNative.BackendFree(_backend);
                        _backend = PrismNative.RegistryCreateBest(_ctx);
                    }
                }
            }
        }

        if (_backend == IntPtr.Zero)
        {
            Log.Error("[AccessibilityMod] PrismHandler: no backend could be acquired.");
            return false;
        }

        _activeBackendName = PrismNative.BackendName(_backend);
        // Cache the feature bitmask: prism_backend_get_features on some
        // backends (notably NVDA) does real work — for NVDA it re-binds an
        // RPC handle and round-trips testIfRunning every call. Features
        // don't change after init, so query once here and re-use.
        _backendFeatures = (PrismNative.BackendFeatures)PrismNative.BackendGetFeatures(_backend);
        Log.Info($"[AccessibilityMod] PrismHandler loaded. Backend: {_activeBackendName ?? "<unknown>"} (features=0x{(ulong)_backendFeatures:X})");
        ApplySavedRateAndVoice();
        return true;
    }

    /// <summary>
    /// Applies the saved Rate/Voice settings to the freshly-acquired backend.
    /// No-ops for backends that don't advertise the relevant feature (e.g.
    /// screen-reader relays like NVDA/JAWS, where rate/voice belong to the
    /// screen reader's own settings, not ours) — in practice this only takes
    /// effect on AVSpeech.
    /// </summary>
    private void ApplySavedRateAndVoice()
    {
        if ((_backendFeatures & PrismNative.BackendFeatures.SupportsSetRate) != 0 && _rateSetting != null)
            PrismNative.BackendSetRate(_backend, _rateSetting.Get() / 100f);

        if ((_backendFeatures & PrismNative.BackendFeatures.SupportsSetVoice) != 0 && _voiceSetting != null)
        {
            if (uint.TryParse(_voiceSetting.Get(), out var id))
                ApplyVoiceById(id);
        }
    }

    /// <summary>
    /// Applies a voice by its numeric index, as returned by the backend's
    /// own voice list (see ProbeVoices). Requires a fresh RefreshVoices call
    /// on this backend instance — the probe backend used to build the
    /// settings menu is a separate handle, so its list isn't shared here.
    /// </summary>
    private void ApplyVoiceById(uint id)
    {
        if (_backend == IntPtr.Zero) return;
        if ((_backendFeatures & PrismNative.BackendFeatures.SupportsSetVoice) == 0) return;

        PrismNative.BackendRefreshVoices(_backend);
        if (PrismNative.BackendCountVoices(_backend, out var count) != PrismNative.PrismError.Ok) return;

        if (id >= count.ToUInt32())
        {
            Log.Error($"[AccessibilityMod] PrismHandler: voice id {id} out of range (count={count}).");
            return;
        }
        PrismNative.BackendSetVoice(_backend, (UIntPtr)id);
    }

    private void RebindBackend()
    {
        if (_ctx == IntPtr.Zero) return;
        if (_backend != IntPtr.Zero)
        {
            try { PrismNative.BackendStop(_backend); } catch { }
            PrismNative.BackendFree(_backend);
            _backend = IntPtr.Zero;
            _backendFeatures = 0;
        }
        AcquireBackend();
    }
}
