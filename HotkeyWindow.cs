using System;
using System.Windows.Forms;

namespace ClipHistory;

/// <summary>不可见的消息窗口，仅用于接收全局热键。</summary>
public sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 0x9001;

    public event Action? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());

        bool ok = NativeMethods.RegisterHotKey(
            Handle,
            HotkeyId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_V);

        if (!ok)
            throw new InvalidOperationException(
                "无法注册热键 Ctrl+Shift+V（可能已被其它程序占用）。");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            DestroyHandle();
        }
    }
}