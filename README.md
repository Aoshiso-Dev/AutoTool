# AutoTool

AutoTool は、Windows 上で定型操作をマクロとして作成・保存・実行するための WPF デスクトップアプリです。  
画面操作（クリック・キー入力）、画像認識、OCR、AI 検出を組み合わせた自動化フローを構築できます。

## 最近の更新（v1.0.6）

- AI 検出の `ラベル名` コンボボックス選択を実行・テスト判定へ確実に反映
- AI 検出テストで一致候補が複数ある場合、すべての候補を赤枠で点滅表示
- 画像検索テストで一致候補が複数ある場合、複数候補を赤枠で点滅表示

## ドキュメント導線

- 利用者向け
  - [利用者向けガイド（リポジトリ）](docs/USER_GUIDE.md)
  - [配布版ガイド（ZIP 同梱）](Readme.txt)
  - [配布版コマンド詳細（ZIP 同梱）](Readme_コマンド詳細.txt)
- 開発者向け
  - [開発者向けガイド](docs/DEVELOPER_GUIDE.md)
  - [アーキテクチャ概要](docs/ARCHITECTURE.md)
  - [クラス図（主要関係）](docs/CLASS_DIAGRAM.md)
  - [配布ガイド](docs/DEPLOYMENT.md)
  - [コマンド開発ガイド](docs/COMMAND_DEVELOPMENT_GUIDE.md)

## GitHub 配布（ZIP）

- 正式バージョンの正は GitHub タグ（`vMAJOR.MINOR.PATCH`）です。
- `v*` タグ（例: `v1.2.3`）を push すると、GitHub Actions の `Release Zip` が実行されます。
- 実行後、GitHub Release に `AutoTool-<tag>-win-x64.zip` が添付されます。
- タグ実行時は publish にタグ由来のバージョンを注入し、生成された `AutoTool.exe` の `FileVersion` / `ProductVersion` がタグと一致することを検証します。
- ZIP には `AutoTool.exe`、必要な `*.dll`、`Readme*.txt`、実行構成ファイルが含まれます。

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
- .NET 10 SDK（C# 14）

詳細は [利用者向けガイド](docs/USER_GUIDE.md) を参照してください。
