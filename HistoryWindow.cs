using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ClipHistory;

public sealed class HistoryWindow : Form
{
    private readonly ClipboardHistory _history;
    private readonly Action<string> _onPaste;
    private readonly WebView2 _web;
    private bool _initialized;

    private static readonly string ThemeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClipHistory", "theme.txt");

    public HistoryWindow(ClipboardHistory history, Action<string> onPaste)
    {
        _history = history;
        _onPaste = onPaste;

        Text = "剪贴板历史";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        Width = 640;
        Height = 500;
        BackColor = Color.FromArgb(30, 30, 32);
        DoubleBuffered = true;

        CenterOnActiveScreen();

        _web = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.Transparent,
            AllowExternalDrop = false
        };
        _web.CoreWebView2InitializationCompleted += OnWebViewInitialized;
        Controls.Add(_web);

        Shown += async (_, _) => await InitWebViewAsync();
        Deactivate += (_, _) => { if (_initialized) Close(); };
    }

    // ============ WebView2 初始化 ============

    private async Task InitWebViewAsync()
    {
        try
        {
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClipHistory", "WebView2");
            Directory.CreateDirectory(userData);

            var env = await CoreWebView2Environment.CreateAsync(null, userData);
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "WebView2 初始化失败：\n" + ex.Message +
                "\n\n本程序需要 Microsoft Edge WebView2 Runtime。\n" +
                "Win11 已内置；Win10 若无，请下载安装：\n" +
                "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                "剪贴板历史", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void OnWebViewInitialized(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            MessageBox.Show("WebView2 初始化失败：" +
                (e.InitializationException?.Message ?? "未知错误"),
                "剪贴板历史", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        var core = _web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsSwipeNavigationEnabled = false;
        core.WebMessageReceived += OnWebMessage;

        var html = LoadEmbeddedHtml("ClipHistory.ui.index.html");
        core.NavigateToString(html);

        _initialized = true;
        BeginInvoke(new Action(() => _web.Focus()));
    }

    private static string LoadEmbeddedHtml(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                "找不到内嵌资源 " + resourceName + "。可用资源：" +
                string.Join(", ", asm.GetManifestResourceNames()));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // ============ 与 JS 通讯 ============

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string json;
        try { json = e.WebMessageAsJson; } catch { return; }
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var t)) return;
            var type = t.GetString();

            switch (type)
            {
                case "ready":
                    SendItems("init");
                    break;

                case "paste":
                    if (root.TryGetProperty("text", out var tp))
                    {
                        var text = tp.GetString() ?? "";
                        Hide();
                        _onPaste(text);
                        Close();
                    }
                    break;

                case "pin":
                    if (root.TryGetProperty("text", out var pinTp))
                    {
                        var text = pinTp.GetString() ?? "";
                        var entry = _history.Items.FirstOrDefault(x => x.Text == text);
                        if (entry != null) _history.TogglePin(entry);
                        SendItems("update", text);
                    }
                    break;

                case "delete":
                    if (root.TryGetProperty("text", out var dp))
                    {
                        var text = dp.GetString() ?? "";
                        var entry = _history.Items.FirstOrDefault(x => x.Text == text);
                        if (entry != null) _history.Remove(entry);
                        SendItems("update");
                    }
                    break;

                case "setTheme":
                    if (root.TryGetProperty("name", out var tn))
                    {
                        var themeName = tn.GetString() ?? "midnight";
                        SaveTheme(themeName);
                    }
                    break;

                case "close":
                    Close();
                    break;
            }
        }
        catch { /* 消息格式异常直接忽略 */ }
    }

    private void SendItems(string type, string? focusText = null)
    {
        CoreWebView2? core;
        try { core = _web.CoreWebView2; } catch { return; }
        if (core == null) return;

        var items = _history.Items.Select(x => new
        {
            text = x.Text,
            time = x.Time.ToString("o"),
            pinned = x.Pinned
        }).ToList();

        var payload = JsonSerializer.Serialize(new
        {
            type,
            theme = LoadTheme(),
            focusText,
            items
        });
        core.PostWebMessageAsJson(payload);
    }

    // ============ 主题持久化 ============

    private static string LoadTheme()
    {
        try
        {
            if (File.Exists(ThemeFile))
            {
                var name = File.ReadAllText(ThemeFile).Trim();
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        catch { }
        return "midnight";
    }

    private static void SaveTheme(string name)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThemeFile)!);
            File.WriteAllText(ThemeFile, name);
        }
        catch { }
    }

    // ============ 窗口定位 + 圆角 ============

    private void CenterOnActiveScreen()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var wa = screen.WorkingArea;
        Left = wa.Left + (wa.Width - Width) / 2;
        Top = wa.Top + (wa.Height - Height) / 3;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRoundCorners();
    }

    private void ApplyRoundCorners()
    {
        // Win11 (Build >= 22000) 用 DWM 原生圆角
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            try
            {
                int pref = DWMWCP_ROUND;
                if (DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE,
                        ref pref, sizeof(int)) == 0)
                    return;
            }
            catch { }
        }

        // 回退：Win10 用 Region 切圆角
        using var path = new GraphicsPath();
        int r = 10;
        var rect = new Rectangle(0, 0, Width, Height);
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    // ============ 清理 ============

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { _web?.Dispose(); } catch { }
        }
        base.Dispose(disposing);
    }
}