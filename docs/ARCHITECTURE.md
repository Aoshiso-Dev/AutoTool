# アーキテクチャ概要

## 1. 全体像

AutoTool は、WPF UI とコマンド実行エンジンを分離した構成です。  
依存関係は「Core（内側）を中心に、Infrastructure（外側）が実装する」形を基本とします。

## 2. レイヤ構成

- `AutoTool`（起動）
  - `IHost` の構築
  - 例外ハンドリング
  - メインウィンドウ表示
- `AutoTool.Desktop`（UI / Application）
  - View / ViewModel
  - 画面操作フロー
  - サービス登録（DI）
- `AutoTool.Automation.Runtime`（Domain/Application 相当）
  - マクロモデル
  - 履歴管理（Undo/Redo）
  - コマンド定義メタデータ
  - ポート定義
- `AutoTool.Automation.Contracts`（契約）
  - コマンド実行コンテキスト
  - コマンド共通インターフェース
- `AutoTool.Infrastructure`（技術実装）
  - Win32 入出力
  - OpenCV / OCR / AI
  - ファイル/ログ/永続化

## 3. 起動シーケンス

1. `AutoTool/App.xaml.cs` で `AppHostBuilder.BuildAndInitialize()` を呼ぶ
2. `AutoTool.Desktop/Hosting/AppHostBuilder.cs` で DI 構成
3. `ICommandRegistry.Initialize()` でコマンド定義を初期化
4. `MainWindow` を生成・表示

## 4. データと設定

- マクロ: `.macro`
- 設定: `Settings\appsettings.json`
- 実行ログ: ログライター経由で出力

## 5. 拡張ポイント

- コマンド定義追加: `CommandDefinition` 属性 + `CommandListItem` 実装
- UI エディタ追加: `CommandProperty` 属性により自動生成
- 外部依存差し替え: `AutoTool.Infrastructure` の実装を DI で差し替え

## 6. テスト戦略

- まず `AutoTool.Automation.Runtime.Tests` でコアロジックを守る
- UI 依存や OS 依存が強い箇所は結合テスト/手動確認で補完