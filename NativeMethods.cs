using System;
using System.Runtime.InteropServices;

namespace ClipHistory;

internal static class NativeMethods
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;
    public const int WM_HOTKEY = 0x0312;

    public const int MOD_ALT     = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT   = 0x0004;
    public const int MOD_WIN     = 0x0008;
    public const int MOD_NOREPEAT = 0x4000;

    public const int VK_V = 0x56;
    public const int VK_CONTROL = 0x11;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
}