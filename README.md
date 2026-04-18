# AutoTool

AutoTool は、Windows 上で定型操作をマクロとして作成・保存・実行するための WPF デスクトップアプリです。  
画面操作（クリック・キー入力）、画像認識、OCR、AI 検出を組み合わせた自動化フローを構築できます。

## ドキュメント

- [利用者向けガイド](docs/USER_GUIDE.md)
- [開発者向けガイド](docs/DEVELOPER_GUIDE.md)
- [アーキテクチャ概要](docs/ARCHITECTURE.md)
- [配布ガイド](docs/DEPLOYMENT.md)
- [コマンド開発ガイド](docs/COMMAND_DEVELOPMENT_GUIDE.md)

## 最短で動かす

```powershell
dotnet restore .\AutoTool.sln
dotnet build .\AutoTool.sln -c Debug
dotnet run --project .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj
```

## テスト実行

```powershell
dotnet test .\AutoTool.Tests.Application\AutoTool.Tests.Application.csproj -c Debug
dotnet test .\AutoTool.Tests.Domain\AutoTool.Tests.Domain.csproj -c Debug
```

## パフォーマンス計測（コマンド生成）

コマンド生成（`MacroFactory.CreateMacro`）のベンチマークは次で実行できます。

```powershell
dotnet run -c Release --project .\AutoTool.Benchmarks.Automation\AutoTool.Benchmarks.Automation.csproj -- --filter *MacroFactoryBenchmarks*
```

## ソリューション構成（要点）

- `AutoTool.Bootstrap`: アプリ起動エントリ（WPF WinExe）
- `AutoTool.Desktop`: UI / ViewModel / Host 構成
- `AutoTool.Application`: ユースケース層（履歴管理、ファイル操作、Ports）
- `AutoTool.Domain`: ドメインモデル
- `AutoTool.Automation.Contracts`: コマンド実行契約・入力/サービス抽象
- `AutoTool.Automation.Runtime`: コマンド定義・マクロ生成・シリアライズ
- `AutoTool.Infrastructure`: Win32 / OpenCV / OCR / 永続化など技術実装
- `AutoTool.Tests.Application`: Application/Runtime の回帰テスト
- `AutoTool.Tests.Domain`: Domain モデルテスト
- `AutoTool.Benchmarks.Automation`: ベンチマーク

## 対応環境

- Windows
- .NET 8 SDK

詳細は [利用者向けガイド](docs/USER_GUIDE.md) を参照してください。