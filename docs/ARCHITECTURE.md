# アーキテクチャ概要

## 1. 目的

このドキュメントは、現在の AutoTool 実装における責務分離と依存方向を短時間で把握するための概要です。

## 2. レイヤ構成（現行）

- `AutoTool.Bootstrap`
  - WPF アプリ起動エントリ
  - `AutoTool.Desktop` を参照し、起動ライフサイクルを開始
- `AutoTool.Desktop`
  - View / ViewModel / 画面イベント制御
  - `AppHostBuilder` で DI 設定
  - WPF 固有 UI アダプタ（ダイアログ、ファイルピッカー、ステータスメッセージ更新）
- `AutoTool.Application`
  - ユースケース（ファイル操作、履歴管理）
  - Port（`IFilePicker` / `ILogWriter` など）定義
- `AutoTool.Domain`
  - ドメインモデル
  - ビジネスルールと不変条件
- `AutoTool.Automation.Contracts`
  - 実行契約（`ICommand`、`ICommandExecutionContext` など）
  - 実行時サービス抽象（`IImageMatcher`、`IOcrEngine` など）
- `AutoTool.Plugin.Abstractions`
  - プラグイン契約
  - プラグイン基本モデル
- `AutoTool.Plugin.Host`
  - `plugin.json` の読込と検証
  - プラグイン探索
  - `entryAssembly` / `entryType` による DLL 読込
- `AutoTool.Automation.Runtime`
  - コマンド定義レジストリ
  - `MacroFactory` によるコマンド木生成
  - `.macro` シリアライズ
- `AutoTool.Infrastructure`
  - Win32 / OpenCV / OCR / AI 実装
  - XML/JSON 永続化、ログ、実行パス解決

## 3. 依存関係（プロジェクト参照ベース）

- `Bootstrap -> Desktop`
- `Desktop -> Application / Automation.Runtime / Infrastructure`
- `Desktop -> Plugin.Abstractions`（将来のプラグインホスト実装時）
- `Desktop -> Plugin.Host`
- `Automation.Runtime -> Application / Automation.Contracts`
- `Plugin.Host -> Plugin.Abstractions`（導入時）
- `Application -> Domain / Automation.Contracts`
- `Infrastructure -> Application / Automation.Runtime / Automation.Contracts`
- `Domain -> (外部プロジェクト参照なし)`

テストプロジェクトは層ごとに分けます。

- `AutoTool.Tests.Domain`: Domain モデルテスト
- `AutoTool.Tests.Application`: Application 層の回帰テスト
- `AutoTool.Tests.Infrastructure`: Infrastructure/Runtime の統合・回帰テスト
- `AutoTool.Tests.Desktop`: Desktop/WPF 境界の回帰テスト

## 4. 起動シーケンス

1. `AutoTool.Bootstrap` がアプリ起動
2. `AutoTool.Desktop/Hosting/AppHostBuilder.cs` で構成/DI を組み立て
3. `ICommandRegistry.Initialize()` でコマンド定義を初期化
4. `MainWindowHostedService` がメインウィンドウを表示

## 5. 設定とデータの配置

- マクロ: `.macro`
- 設定: `Settings\appsettings.json`
- UI 状態: `Settings\window_settings.json`
- 最近使ったファイル: `Settings\RecentFiles_*.xml`
- お気に入り: `Settings\favorites.xml`
- ログ: `ILogWriter` 経由（実体は `Infrastructure` 側）

## 6. 方針（AGENTS 準拠で重要な点）

- `Application` / `Domain` から `System.Windows` / `Microsoft.Win32` / `AutoTool.Desktop` を参照しない
- `DllImport` / `LibraryImport` は `Infrastructure` のみに置く
- WPF 固有アダプタ（ダイアログ、Dispatcher、ファイルピッカー）は `Desktop` に置く
- `Application` / `Domain` テストも UI や Infrastructure へ直接依存させず、必要な検証は専用テストプロジェクトへ分ける
- Service Locator（`IServiceProvider` 直参照、`GetService(...)`）を使わず、コンストラクタ DI を使う

## 7. 補足: 旧プロジェクト

リポジトリには `AutoTool.Commands` / `AutoTool.Core.Tests` / `AutoTool.Core.Benchmarks` も存在しますが、`AutoTool.sln` の現行構成には含めていません。現行の改修・検証は `AutoTool.sln` 採用プロジェクトを正として進めます。


