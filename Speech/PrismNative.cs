using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SayTheSpire2.Speech;

/// <summary>
/// Thin P/Invoke layer over prism.dll. Mirrors the subset of the Prism C API
/// (see prism/doc/src/api/) that the speech handler needs: context lifecycle,
/// registry enumeration, and per-backend speak / output / stop / free.
/// </summary>
internal static class PrismNative
{
    private const string Dll = "prism";

    public enum PrismError : int
    {
        Ok = 0,
        AlreadyInitialized = 1,
        BackendNotAvailable = 2,
        Internal = 3,
        MemoryFailure = 4,
        Unknown = 5,
        NotInitialized = 6,
        InvalidUtf8 = 7,
        SpeakFailure = 8,
        NotImplemented = 9,
        InternalBackendLimitExceeded = 10,
    }

    [Flags]
    public enum BackendFeatures : ulong
    {
        SupportedAtRuntime = 1UL << 0,
        SupportsSpeak = 1UL << 2,
        SupportsSpeakToMemory = 1UL << 3,
        SupportsBraille = 1UL << 4,
        SupportsOutput = 1UL << 5,
        SupportsIsSpeaking = 1UL << 6,
        SupportsStop = 1UL << 7,
    }

    [DllImport(Dll, EntryPoint = "prism_init")]
    public static extern IntPtr Init(IntPtr config);

    [DllImport(Dll, EntryPoint = "prism_shutdown")]
    public static extern void Shutdown(IntPtr ctx);

    [DllImport(Dll, EntryPoint = "prism_registry_count")]
    public static extern UIntPtr RegistryCount(IntPtr ctx);

    [DllImport(Dll, EntryPoint = "prism_registry_id_at")]
    public static extern ulong RegistryIdAt(IntPtr ctx, UIntPtr index);

    [DllImport(Dll, EntryPoint = "prism_registry_name")]
    private static extern IntPtr RegistryNameRaw(IntPtr ctx, ulong id);

    public static string? RegistryName(IntPtr ctx, ulong id) =>
        Utf8FromPtr(RegistryNameRaw(ctx, id));

    [DllImport(Dll, EntryPoint = "prism_registry_create")]
    public static extern IntPtr RegistryCreate(IntPtr ctx, ulong id);

    [DllImport(Dll, EntryPoint = "prism_registry_create_best")]
    public static extern IntPtr RegistryCreateBest(IntPtr ctx);

    [DllImport(Dll, EntryPoint = "prism_backend_initialize")]
    public static extern PrismError BackendInitialize(IntPtr backend);

    [DllImport(Dll, EntryPoint = "prism_backend_free")]
    public static extern void BackendFree(IntPtr backend);

    [DllImport(Dll, EntryPoint = "prism_backend_get_features")]
    public static extern ulong BackendGetFeatures(IntPtr backend);

    [DllImport(Dll, EntryPoint = "prism_backend_name")]
    private static extern IntPtr BackendNameRaw(IntPtr backend);

    public static string? BackendName(IntPtr backend) =>
        Utf8FromPtr(BackendNameRaw(backend));

    [DllImport(Dll, EntryPoint = "prism_backend_speak")]
    private static extern PrismError BackendSpeakRaw(IntPtr backend, byte[] textUtf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

    public static PrismError BackendSpeak(IntPtr backend, string text, bool interrupt) =>
        BackendSpeakRaw(backend, Utf8(text), interrupt);

    [DllImport(Dll, EntryPoint = "prism_backend_output")]
    private static extern PrismError BackendOutputRaw(IntPtr backend, byte[] textUtf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

    public static PrismError BackendOutput(IntPtr backend, string text, bool interrupt) =>
        BackendOutputRaw(backend, Utf8(text), interrupt);

    [DllImport(Dll, EntryPoint = "prism_backend_stop")]
    public static extern PrismError BackendStop(IntPtr backend);

    [DllImport(Dll, EntryPoint = "prism_error_string")]
    private static extern IntPtr ErrorStringRaw(PrismError err);

    public static string? ErrorString(PrismError err) =>
        Utf8FromPtr(ErrorStringRaw(err));

    private static byte[] Utf8(string s)
    {
        // Native side expects null-terminated UTF-8.
        var len = Encoding.UTF8.GetByteCount(s);
        var buf = new byte[len + 1];
        Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
        return buf;
    }

    private static string? Utf8FromPtr(IntPtr ptr) =>
        ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
}
