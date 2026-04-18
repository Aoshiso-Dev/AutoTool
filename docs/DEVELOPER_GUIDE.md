# 開発者向けガイド

## 1. 目的

このガイドは、AutoTool ソリューションを安全に改修するための最短手順をまとめたものです。

## 2. 開発環境

- Windows
- .NET 8 SDK
- Visual Studio 2022 もしくは `dotnet` CLI

## 3. よく使うコマンド

```powershell
dotnet restore .\AutoTool.sln
dotnet build .\AutoTool.sln -c Debug
dotnet test .\AutoTool.Core.Tests\AutoTool.Core.Tests.csproj -c Debug
```

アプリ実行:

```powershell
dotnet run --project .\AutoTool\AutoTool.csproj
```

## 4. プロジェクト責務

- `AutoTool`
  - 起動エントリ（`App.xaml.cs`）
  - `IHost` 構築とメインウィンドウ起動
- `AutoTool.Desktop`
  - 画面（XAML）
  - ViewModel
  - DI 登録のアプリ層
- `AutoTool.Core`
  - マクロ編集/履歴/シリアライズなどの中核ロジック
  - ポート（インターフェース）定義
- `AutoTool.Infrastructure`
  - Win32、OpenCV、OCR、AI、XML 永続化など外部依存実装
- `AutoTool.Commands.Abstractions`
  - コマンド実行コンテキスト・共通契約
- `AutoTool.Commands`
  - コマンド群

## 5. 改修時の基本方針

- 小さく変更し、`build` と `test` をこまめに通す
- Core 層は外部実装に直接依存させない
- UI 変更時は保存・実行・ログ表示まで動作確認する

## 6. 新しいコマンドを追加する

コマンド追加の実装ルールと実例は次を参照してください。

- [コマンド開発ガイド](COMMAND_DEVELOPMENT_GUIDE.md)

## 7. 変更時のチェックリスト

- ビルド成功
- `AutoTool.Core.Tests` 成功
- 主要操作（開く/保存/実行/Undo/Redo）確認
- 例外発生時にログ/通知が正しく出ることを確認

## 8. 関連ドキュメント

- [アーキテクチャ概要](ARCHITECTURE.md)
- [配布ガイド](DEPLOYMENT.md)