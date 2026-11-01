using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClipHistory;

public sealed class TrayApp : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppKey = "ClipHistory";

    private readonly NotifyIcon _tray;
    private readonly ClipboardHistory _history;
    private readonly ClipboardMonitor _monitor;
    private readonly HotkeyWindow _hotkey;

    private HistoryWindow? _popup;
    private IntPtr _previousWindow = IntPtr.Zero;

    public TrayApp()
    {
        _history = new ClipboardHistory(200);

        _monitor = new ClipboardMonitor();
        _monitor.TextCopied += text => _history.Add(text);

        _hotkey = new HotkeyWindow();
        _hotkey.HotkeyPressed += TogglePopup;

        _tray = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Visible = true,
            Text = "剪贴板历史 - Ctrl+Shift+V"
        };
        _tray.DoubleClick += (_, _) => TogglePopup();
        _tray.ContextMenuStrip = BuildMenu();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("显示历史 (Ctrl+Shift+V)", null, (_, _) => ShowPopup());
        menu.Items.Add(new ToolStripSeparator());

        var autoStart = new ToolStripMenuItem("开机自启")
        {
            CheckOnClick = true,
            Checked = IsAutoStartEnabled()
        };
        autoStart.Click += (_, _) =>
        {
            try { SetAutoStart(autoStart.Checked); }
            catch (Exception ex)
            {
                MessageBox.Show("设置失败：" + ex.Message);
                autoStart.Checked = IsAutoStartEnabled();
            }
        };
        menu.Items.Add(autoStart);

        menu.Items.Add("清空历史", null, (_, _) =>
        {
            var pinnedCount = _history.PinnedCount;
            if (pinnedCount > 0)
            {
                var r = MessageBox.Show(
                    $"清空未收藏的剪贴板历史？\n\n（{pinnedCount} 条已收藏的记录会保留）",
                    "清空历史", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes) _history.Clear(keepPinned: true);
            }
            else
            {
                var r = MessageBox.Show(
                    "确定清空全部剪贴板历史？",
                    "清空历史", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes) _history.Clear(keepPinned: false);
            }
        });

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private void TogglePopup()
    {
        if (_popup is { IsDisposed: false, Visible: true })
        {
            _popup.Close();
            _popup = null;
            return;
        }
        ShowPopup();
    }

    private void ShowPopup()
    {
        _popup?.Close();
        _previousWindow = NativeMethods.GetForegroundWindow();

        _popup = new HistoryWindow(_history, PasteText);
        _popup.FormClosed += (_, _) => _popup = null;
        _popup.Show();
        _popup.Activate();
    }

    private void PasteText(string text)
    {
        try
        {
            _monitor.SuppressNext = true;
            Clipboard.SetDataObject(text, true);
        }
        catch
        {
            _monitor.SuppressNext = false;
            return;
        }

        Thread.Sleep(60);

        if (_previousWindow != IntPtr.Zero)
        {
            try
            {
                uint targetThread = NativeMethods.GetWindowThreadProcessId(_previousWindow, out _);
                uint curThread = NativeMethods.GetCurrentThreadId();
                if (targetThread != curThread)
                    NativeMethods.AttachThreadInput(curThread, targetThread, true);

                NativeMethods.SetForegroundWindow(_previousWindow);

                if (targetThread != curThread)
                    NativeMethods.AttachThreadInput(curThread, targetThread, false);

                Thread.Sleep(60);
            }
            catch { }
        }

        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);

        _previousWindow = IntPtr.Zero;
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppKey) != null;
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKey);

        if (enabled)
            key.SetValue(AppKey, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(AppKey, throwOnMissingValue: false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _monitor.Dispose();
            _hotkey.Dispose();
            _popup?.Dispose();
        }
        base.Dispose(disposing);
    }
}