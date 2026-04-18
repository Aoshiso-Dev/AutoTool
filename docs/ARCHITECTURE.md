# アーキテクチャ概要

## 1. 全体像

AutoTool は、`AutoTool.Bootstrap` を起点に UI / Application / Domain / Infrastructure を分離した構成です。  
現在の実装は、リファクタリング後の以下レイヤを中心に構成されています。

## 2. レイヤ構成

- `AutoTool.Bootstrap`
  - WPF アプリ起動エントリ
  - `IHost` 構築とライフサイクル管理
- `AutoTool.Desktop`
  - View / ViewModel
  - 画面イベントとユースケース呼び出し
  - DI 登録（Host 構成）
- `AutoTool.Application`
  - 履歴管理（Undo/Redo）
  - ファイル操作ユースケース
  - Infrastructure が実装する Port 抽象
- `AutoTool.Domain`
  - ドメインモデルと不変条件
- `AutoTool.Automation.Contracts`
  - コマンド実行契約（`ICommandExecutionContext` など）
  - 入力モデル・サービス抽象
- `AutoTool.Automation.Runtime`
  - コマンド定義メタデータ
  - マクロ生成（Factory）
  - シリアライズ
- `AutoTool.Infrastructure`
  - Win32 入出力
  - OpenCV / OCR / AI
  - XML 永続化、ログ、パス解決

## 3. 依存関係（実装上）

- `Bootstrap -> Desktop`
- `Desktop -> Application / Automation.Runtime / Infrastructure`
- `Automation.Runtime -> Application / Automation.Contracts`
- `Application -> Domain / Automation.Contracts`
- `Domain -> (no external project references)`
- `Infrastructure -> Application / Automation.Runtime / Automation.Contracts`

## 4. 起動シーケンス

1. `AutoTool.Bootstrap/App.xaml.cs` で Host を構築
2. `AutoTool.Desktop/Hosting/AppHostBuilder.cs` で設定と DI を登録
3. `CommandRegistryInitializationHostedService` が `ICommandRegistry.Initialize()` を実行
4. `MainWindowHostedService` がメインウィンドウを起動

## 5. データと設定

- マクロ: `.macro`
- 設定: `Settings\appsettings.json`
- 実行ログ: `ILogWriter` 経由で出力

## 6. 拡張ポイント

- コマンド定義追加: `CommandDefinition` 属性 + `CommandListItem` 実装
- UI エディタ追加: `CommandProperty` 属性により編集 UI を自動生成
- 外部依存差し替え: `AutoTool.Infrastructure` 実装を DI で差し替え

## 7. テスト戦略

- `AutoTool.Tests.Domain`: ドメイン不変条件の回帰を検証
- `AutoTool.Tests.Application`: 履歴管理・ファイル操作・DI 方針の回帰を検証
- UI 依存や OS 依存が強い箇所は結合テスト/手動確認で補完
