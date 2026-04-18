# AutoTool

AutoTool は、Windows 上で定型操作をマクロとして作成・保存・実行するための WPF デスクトップアプリです。  
画面操作（クリック・キー入力）、画像認識、OCR、AI 検出などを組み合わせて自動化フローを構築できます。

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
dotnet run --project .\AutoTool\AutoTool.csproj
```

## パフォーマンス計測（コマンド生成）

コマンド生成（`MacroFactory.CreateMacro`）のベンチマークは次で実行できます。

```powershell
dotnet run -c Release --project .\AutoTool.Automation.Runtime.Benchmarks\AutoTool.Automation.Runtime.Benchmarks.csproj -- --filter *MacroFactoryBenchmarks*
```

既定では `100 / 1000 / 10000` 件のコマンド規模で計測します。

## ソリューション構成（要点）

- `AutoTool`: アプリ起動エントリ（WPF WinExe）
- `AutoTool.Desktop`: 画面・ViewModel・ホスト構成
- `AutoTool.Automation.Runtime`: 業務ロジック・モデル・ポート
- `AutoTool.Infrastructure`: Win32 / OpenCV / OCR / 永続化などの実装
- `AutoTool.Automation.Contracts`: コマンド実行の契約・基底型
- `AutoTool.Commands`: コマンド層
- `AutoTool.Automation.Runtime.Tests`: コアロジックのテスト
- `AutoTool.Automation.Runtime.Benchmarks`: コマンド生成パフォーマンス計測

## 対応環境

- Windows
- .NET 8 SDK

詳細手順は [利用者向けガイド](docs/USER_GUIDE.md) を参照してください。
