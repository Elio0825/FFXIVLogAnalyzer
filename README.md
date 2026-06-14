# FFXIVLogAnalyzer

FFXIVLogAnalyzer 是一款基于 .NET 8 和 WPF 的 Windows 桌面工具，用于分析 FF14 / FFXIV 战斗日志，辅助查看战斗过程、技能记录和相关统计信息。

本项目是非官方工具，不隶属于 Square Enix，也不代表 Square Enix。

## 功能概览

- 读取并解析本地战斗日志文件
- 展示战斗会话和日志明细
- 支持查看原始日志内容
- 根据技能 ID 显示技能名称
- 支持手动维护技能名称映射
- Release 配置支持发布为 Windows 单文件 exe

## 运行环境

- Windows 10 或更高版本
- .NET 8 Runtime

如果使用项目自带的单文件发布产物，通常不需要用户额外安装 .NET Runtime。

## 开发环境

- Windows
- .NET 8 SDK
- Visual Studio 2022 或其他支持 .NET 8 / WPF 的 IDE

推荐从根目录打开解决方案文件：

```text
FFXIVLogAnalyzer.slnx
```

项目文件位于：

```text
FFXIVLogAnalyzer\FFXIVLogAnalyzer.csproj
```

## 快速构建

在仓库根目录执行：

```powershell
dotnet build .\FFXIVLogAnalyzer\FFXIVLogAnalyzer.csproj
```

## 发布单文件 exe

在仓库根目录执行：

```powershell
dotnet publish .\FFXIVLogAnalyzer\FFXIVLogAnalyzer.csproj -c Release
```

发布后的程序位于：

```text
FFXIVLogAnalyzer\bin\Release\net8.0-windows\win-x64\publish\FFXIVLogAnalyzer.exe
```

Release 配置中启用了：

- `SelfContained`
- `PublishSingleFile`
- `EnableCompressionInSingleFile`

因此发布目录中的 `FFXIVLogAnalyzer.exe` 是可独立运行的单文件程序。

## 技能名称数据

程序启动时会读取内置的 `Action.csv` 资源，用于把技能 ID 映射为技能名称。

当前仓库包含：

```text
FFXIVLogAnalyzer\Resources\Action.csv
```

该文件会在构建时作为嵌入资源打包进程序。读取逻辑会使用 CSV 的前两列：

```csv
Id,Name
100001,Example Skill Name
```

如果你需要替换或更新技能名称数据，请用新的 `Action.csv` 覆盖：

```text
FFXIVLogAnalyzer\Resources\Action.csv
```

然后重新构建或发布项目。

注意：如果 `Action.csv` 来自游戏数据导出或第三方数据源，请在公开分发前确认你拥有相应的使用和再分发权限。

## 本地用户数据

程序会把用户手动维护的技能映射保存到当前 Windows 用户的应用数据目录：

```text
%APPDATA%\FFXIVLogAnalyzer\skill_mappings.json
```

该文件是用户本地配置，不会被写入仓库。

## 仓库结构

```text
.
├─ .github\workflows\dotnet.yml
├─ FFXIVLogAnalyzer.slnx
├─ FFXIVLogAnalyzer
│  ├─ Converters
│  ├─ Models
│  ├─ Resources
│  │  ├─ Action.csv
│  │  ├─ Action.csv.example
│  │  └─ app.ico
│  ├─ Services
│  ├─ ViewModels
│  ├─ App.xaml
│  ├─ MainWindow.xaml
│  └─ FFXIVLogAnalyzer.csproj
├─ .gitattributes
├─ .gitignore
├─ LICENSE
└─ README.md
```

## GitHub Actions

仓库包含 GitHub Actions 工作流：

```text
.github\workflows\dotnet.yml
```

当推送到 `main` 分支或创建 Pull Request 时，会自动执行：

- `dotnet restore`
- `dotnet build -c Release`
- `dotnet publish -c Release`

## 不应提交的内容

以下内容不应提交到仓库：

- `bin/`
- `obj/`
- `.vs/`
- `.dotnet_home/`
- 发布生成的 exe
- 调试符号文件
- 崩溃日志
- 包含玩家隐私信息的真实战斗日志

这些规则已经写入 `.gitignore`。

## 隐私说明

请不要公开提交包含真实玩家信息、队伍信息、服务器信息或其他隐私内容的战斗日志。提交 issue 或反馈问题时，建议先脱敏日志内容。

## License

项目源码使用 MIT License。详见 [LICENSE](LICENSE)。

项目中包含的外部数据、游戏相关名称、图标或其他资源可能受其原始权利方约束。请在分发前确认相关授权。
