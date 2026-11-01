using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ClipHistory;

/// <summary>通过 AddClipboardFormatListener 监听剪贴板变化。</summary>
public sealed class ClipboardMonitor : NativeWindow, IDisposable
{
    public event Action<string>? TextCopied;

    /// <summary>置为 true 时忽略下一次变化（自己写入剪贴板时使用）。</summary>
    public bool SuppressNext { get; set; }

    public ClipboardMonitor()
    {
        CreateHandle(new CreateParams());
        if (!NativeMethods.AddClipboardFormatListener(Handle))
            throw new InvalidOperationException("无法注册剪贴板监听");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            if (SuppressNext)
            {
                SuppressNext = false;
            }
            else
            {
                TryRead();
            }
        }
        base.WndProc(ref m);
    }

    private void TryRead()
    {
        // 剪贴板可能被其它进程占用，重试几次
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text))
                        TextCopied?.Invoke(text);
                }
                return;
            }
            catch (ExternalException)
            {
                Thread.Sleep(20);
            }
            catch
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(Handle);
            DestroyHandle();
        }
    }
}