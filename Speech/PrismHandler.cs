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
    private CategorySetting? _settings;
    private ChoiceSetting? _backendSetting;

    public string Key => "prism";
    public string Label => "Prism";

    public CategorySetting? GetSettings()
    {
        if (_settings != null) return _settings;

        _settings = new CategorySetting(Key, Label);

        var choices = new List<Choice> { new Choice(AutoBackend, "Auto (Best Available)") };
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
                    }
                    finally { PrismNative.BackendFree(backend); }
                }
            }
            finally { PrismNative.Shutdown(probeCtx); }
        }

        _backendSetting = new ChoiceSetting("backend", "Backend", AutoBackend, choices, localizationKey: "SPEECH.PRISM.BACKEND");
        _settings.Add(_backendSetting);
        _backendSetting.Changed += _ => RebindBackend();

        return _settings;
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
            // to plain speak so we still produce audio.
            var features = (PrismNative.BackendFeatures)PrismNative.BackendGetFeatures(_backend);
            if ((features & PrismNative.BackendFeatures.SupportsOutput) != 0)
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
        Log.Info($"[AccessibilityMod] PrismHandler loaded. Backend: {_activeBackendName ?? "<unknown>"}");
        return true;
    }

    private void RebindBackend()
    {
        if (_ctx == IntPtr.Zero) return;
        if (_backend != IntPtr.Zero)
        {
            try { PrismNative.BackendStop(_backend); } catch { }
            PrismNative.BackendFree(_backend);
            _backend = IntPtr.Zero;
        }
        AcquireBackend();
    }
}
