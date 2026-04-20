# 開発者向けガイド

## 1. 目的

このガイドは、AutoTool を安全に改修し、配布まで一貫して進めるための実行手順をまとめたものです。

## 2. 前提環境

- Windows
- .NET 10 SDK
- C# 14（`LangVersion=14.0`）
- Visual Studio 2022 または `dotnet` CLI

## 3. まず確認すること

- ルート `AGENTS.md` を確認し、実装・配布・文書同期ポリシーに従う
- 変更対象がユーザー向けに影響する場合は、同一変更内で `docs/*.md` と `Readme*.txt` の更新要否を判断する

## 4. よく使うコマンド

依存関係復元・ビルド:

```powershell
dotnet restore .\AutoTool.sln
dotnet build .\AutoTool.sln -c Release
```

テスト:

```powershell
dotnet test .\AutoTool.Tests.Application\AutoTool.Tests.Application.csproj -c Release --no-build
dotnet test .\AutoTool.Tests.Domain\AutoTool.Tests.Domain.csproj -c Release --no-build
```

アプリ実行:

```powershell
dotnet run --project .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj
```

## 5. プロジェクト責務（現行 `AutoTool.sln`）

- `AutoTool.Bootstrap`
  - 起動エントリ（WPF）
- `AutoTool.Desktop`
  - 画面、ViewModel、Host 構成、WPF 固有アダプタ
- `AutoTool.Application`
  - ユースケース、履歴管理、Port 抽象
- `AutoTool.Domain`
  - ドメインモデルと不変条件
- `AutoTool.Automation.Contracts`
  - 実行契約・サービス抽象
- `AutoTool.Automation.Runtime`
  - コマンド定義、マクロ生成、シリアライズ
- `AutoTool.Infrastructure`
  - Win32/OpenCV/OCR/AI、ファイル永続化、ログなど技術実装
- `AutoTool.Tests.Application`
  - Application/Runtime 回帰テスト
- `AutoTool.Tests.Domain`
  - Domain モデルテスト
- `AutoTool.Benchmarks.Automation`
  - ベンチマーク

## 6. 改修時の基本方針

- 小さく変更し、`build`/`test` をこまめに通す
- `Application` / `Domain` に WPF 依存を持ち込まない
- `DllImport` / `LibraryImport` は `Infrastructure` に限定する
- Service Locator を使わず、コンストラクタ DI を使う
- UI変更時は、開く/保存/実行/停止/Undo/Redo/ログ表示を確認する

## 7. 配布まで行う場合

標準手順:

1. `dotnet build -c Release`
2. 必要なら `dotnet test -c Release --no-build`
3. `deploy-to-c-autotool.ps1` で publish とコピーを直列実行
4. `C:\AutoTool` 側で主要バイナリと設定ファイルを確認

実行例:

```powershell
.\deploy-to-c-autotool.ps1 -Destination C:\AutoTool
```

## 8. ドキュメント更新チェック

- 実装/仕様/挙動を変更した
- 配布手順、制約、コマンド説明が変わった
- 画面表示文言、利用者判断に関わる内容が変わった

上記に該当する場合は次を同一変更で更新する:

- `README.md`
- `docs/*.md`
- `Readme.txt`
- `Readme_コマンド詳細.txt`

## 9. 関連ドキュメント

- [アーキテクチャ概要](ARCHITECTURE.md)
- [クラス図（主要関係）](CLASS_DIAGRAM.md)
- [配布ガイド](DEPLOYMENT.md)
- [コマンド開発ガイド](COMMAND_DEVELOPMENT_GUIDE.md)
- [利用者向けガイド](USER_GUIDE.md)
