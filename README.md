# FFXIVLogAnalyzer

FFXIVLogAnalyzer 是一个基于 .NET 8 和 WPF 的 Windows 桌面工具，用于分析 FFXIV 日志数据。

## 环境要求

- Windows
- .NET 8 SDK

## 构建

```powershell
dotnet build .\FFXIVLogAnalyzer\FFXIVLogAnalyzer.csproj
```

也可以使用根目录的 `FFXIVLogAnalyzer.slnx` 在 Visual Studio 中打开项目。

## 发布单文件程序

```powershell
dotnet publish .\FFXIVLogAnalyzer\FFXIVLogAnalyzer.csproj -c Release
```

发布后的单文件 exe 位于：

```text
FFXIVLogAnalyzer\bin\Release\net8.0-windows\win-x64\publish\FFXIVLogAnalyzer.exe
```

## 技能名称映射数据

项目会在启动时读取内置的 `Action.csv` 资源，用于加载技能 ID 到技能名称的默认映射。

开源仓库默认不包含完整游戏数据文件。如果需要启用默认映射，请将 CSV 文件放到：

```text
FFXIVLogAnalyzer\Resources\Action.csv
```

CSV 前两列格式如下：

```csv
Id,Name
100001,Example Skill Name
```

如果该文件不存在，程序仍可编译和运行，只是不会预加载默认技能名称映射。

## 开源注意事项

- 本项目是非官方工具，不隶属于或代表 Square Enix。
- 不要提交 `bin/`、`obj/`、发布 exe、日志文件或崩溃日志。
- 不要提交包含玩家隐私信息的真实战斗日志。
- 请确认图标、截图和外部数据文件拥有可分发授权。

## GitHub Actions

仓库包含 `.github/workflows/dotnet.yml`，推送到 `main` 分支或提交 Pull Request 时会自动执行：

- `dotnet restore`
- `dotnet build -c Release`
- `dotnet publish -c Release`

## License

本项目使用 MIT License。详见 [LICENSE](LICENSE)。
