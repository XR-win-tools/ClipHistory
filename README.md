# ClipHistory

> Windows 轻量级剪贴板历史管理器 · 全局热键 `Ctrl+Shift+V` 一键呼出 · 纯本地存储

一个用 .NET 8 + WinForms 写的剪贴板增强工具。所有复制过的文本都会自动保存，
需要时按一下热键就能翻出来，选中即粘贴回原窗口。

## ✨ 功能

- 🔥 全局热键 `Ctrl+Shift+V` 呼出历史面板
- 📋 自动记录文本剪贴板内容（去重、最新置顶）
- 🔍 输入即搜，实时过滤
- ⌨️ `↑↓` 选择 / `Enter` 粘贴 / `Delete` 删除 / `Esc` 关闭
- 🚀 选中后自动切回原窗口并模拟 Ctrl+V 粘贴
- 💾 历史本地持久化到 `%AppData%\ClipHistory\history.json`（最多 200 条）
- 🖥️ 系统托盘常驻、开机自启、单实例运行
- 🔒 完全离线，不含任何网络请求，不上传任何数据

## 📥 下载安装

1. 打开 [Releases](../../releases/latest) 页面
2. 下载 `ClipHistory-win-x64.zip`
3. 解压出 `ClipHistory.exe`，放到你喜欢的位置（比如 `C:\Tools\`）
4. 双击运行，托盘区会出现图标

> 无需安装 .NET 或任何依赖。支持 Windows 10 / 11 64 位。
> 首次运行 Windows 可能提示"已保护你的电脑"，
> 点 **更多信息 → 仍要运行** 即可（个人签名项目未做代码签名）。

## 🎯 使用方法

| 操作 | 说明 |
|------|------|
| `Ctrl+Shift+V` | 呼出 / 关闭历史面板 |
| 面板里输入文字 | 实时搜索过滤 |
| `↑` / `↓` | 移动选中项 |
| `Enter` / 双击 | 粘贴到刚才的窗口 |
| `Delete` | 删除选中项 |
| `Esc` / 点空白处 | 关闭面板 |
| 托盘图标右键 | 显示历史 / 开机自启 / 清空历史 / 退出 |

## 🛠 从源码构建

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```bash
git clone https://github.com/YOURNAME/ClipHistory.git
cd ClipHistory
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

产物在 `bin\Release\net8.0-windows\win-x64\publish\ClipHistory.exe`。

## ❓ 常见问题

**按热键没反应？** 可能被其它程序占用了（微信、输入法、IDE 常见）。
程序仍在托盘运行，右键 → 显示历史即可呼出。

**历史保存到哪里？** `%AppData%\ClipHistory\history.json`。想迁移/备份拷这个文件即可。

**会不会上传我的剪贴板？** 不会。源码里没有任何网络相关代码，历史只存在你自己电脑上。

**怎么彻底卸载？** 托盘右键 → 取消"开机自启" → 退出 → 删除 exe 和 `%AppData%\ClipHistory\` 目录。

## 📄 许可

[MIT License](LICENSE)