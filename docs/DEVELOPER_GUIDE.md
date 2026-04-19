# 開発者向けガイド

## 1. 目的

このガイドは、AutoTool ソリューションを安全に改修するための実行手順をまとめたものです。

## 2. 開発環境

- Windows
- .NET 10 SDK
- C# 14（`LangVersion=14.0`）
- Visual Studio 2022 または `dotnet` CLI

## 3. よく使うコマンド

```powershell
dotnet restore .\AutoTool.sln
dotnet build .\AutoTool.sln -c Debug
dotnet test .\AutoTool.Tests.Application\AutoTool.Tests.Application.csproj -c Debug
dotnet test .\AutoTool.Tests.Domain\AutoTool.Tests.Domain.csproj -c Debug
```

アプリ実行:

```powershell
dotnet run --project .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj
```

## 4. プロジェクト責務

- `AutoTool.Bootstrap`
  - 起動エントリ（`App.xaml.cs`）
  - `IHost` 構築とアプリライフサイクル
- `AutoTool.Desktop`
  - 画面（XAML）
  - ViewModel
  - Host/DI 構成
- `AutoTool.Application`
  - 履歴管理
  - ファイル操作
  - Port 抽象
- `AutoTool.Domain`
  - ドメインモデル
  - 不変条件
- `AutoTool.Automation.Contracts`
  - コマンド実行コンテキスト契約
  - 入力モデル・サービス抽象
- `AutoTool.Automation.Runtime`
  - コマンド定義・マクロ生成・シリアライズ
- `AutoTool.Infrastructure`
  - Win32、OpenCV、OCR、AI、XML 永続化など技術実装
- `AutoTool.Tests.Application`
  - Application/Runtime の回帰テスト
- `AutoTool.Tests.Domain`
  - Domain モデルの単体テスト

## 5. 改修時の基本方針

- 小さく変更し、`build` と `test` をこまめに通す
- ドメインルールは Domain/Application 側へ集約する
- UI 変更時は開く/保存/実行/Undo/Redo/ログ表示まで確認する

## 6. 新しいコマンドを追加する

- [コマンド開発ガイド](COMMAND_DEVELOPMENT_GUIDE.md)

## 7. 変更時のチェックリスト

- ビルド成功
- `AutoTool.Tests.Application` 成功
- `AutoTool.Tests.Domain` 成功
- 主要操作（開く/保存/実行/Undo/Redo）確認
- 例外発生時にログ/通知が正しく出ることを確認

## 8. 関連ドキュメント

- [アーキテクチャ概要](ARCHITECTURE.md)
- [配布ガイド](DEPLOYMENT.md)
