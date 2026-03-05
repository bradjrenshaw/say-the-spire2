using System;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Sts2AccessibilityMod.Patches;

/// <summary>
/// Disables Godot's built-in AccessKit accessibility by subclassing the game window
/// and intercepting WM_GETOBJECT messages. This prevents Windows UI Automation from
/// querying the accessibility tree, which stops AccessKit from driving focus, input,
/// and screen reader output.
/// </summary>
public static class DisableBuiltinAccessibility
{
    private const int WM_GETOBJECT = 0x003D;
    private const int GWLP_WNDPROC = -4;

    private static IntPtr _originalWndProc;
    private static WndProcDelegate? _wndProcDelegate;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public static void Initialize()
    {
        try
        {
            // Get the game's native window handle from Godot
            var hwnd = (IntPtr)(long)DisplayServer.WindowGetNativeHandle(
                DisplayServer.HandleType.WindowHandle);

            if (hwnd == IntPtr.Zero)
            {
                Log.Error("[AccessibilityMod] Could not get window handle.");
                return;
            }

            Log.Info($"[AccessibilityMod] Window handle: 0x{hwnd:X}");

            // Keep a reference to the delegate so it doesn't get GC'd
            _wndProcDelegate = WndProc;
            var newWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

            _originalWndProc = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, newWndProc);

            if (_originalWndProc == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Log.Error($"[AccessibilityMod] SetWindowLongPtr failed, error: {error}");
                return;
            }

            Log.Info("[AccessibilityMod] Window subclassed - WM_GETOBJECT will be blocked.");
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to disable built-in accessibility: {ex}");
        }
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETOBJECT)
        {
            // Return 0 to tell Windows "no accessibility provider here"
            return IntPtr.Zero;
        }

        return CallWindowProcW(_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
