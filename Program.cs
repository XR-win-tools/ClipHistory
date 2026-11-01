using System;
using System.Threading;
using System.Windows.Forms;

namespace ClipHistory;

internal static class Program
{
    private const string MutexName = "Global\\ClipHistory.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("剪贴板历史已在运行（Ctrl+Shift+V 呼出）。",
                "剪贴板历史", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        try
        {
            using var context = new TrayApp();
            Application.Run(context);
        }
        catch (Exception ex)
        {
            MessageBox.Show("启动失败：" + ex.Message, "剪贴板历史",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}