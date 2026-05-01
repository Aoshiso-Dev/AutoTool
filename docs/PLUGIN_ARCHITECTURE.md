# プラグインアーキテクチャ仕様

## 1. 目的

このドキュメントは、`AutoTool` に後付けで機能を追加するためのプラグイン機能について、全体方針と基本仕様を定義します。

本仕様の狙いは、アプリ本体の責務を増やしすぎずに拡張性を確保し、追加機能を安全かつ段階的に導入できるようにすることです。

## 2. 基本方針

- `AutoTool` 本体にはプラグイン基盤のみを持たせる
- 追加機能はプラグインとして分離し、本体と疎結合に保つ
- 既存のコマンド定義方式と整合する形で拡張する
- `Application` / `Domain` に UI 依存や技術詳細を持ち込まない
- プラグイン未導入時でも既存機能と既存マクロが壊れないようにする
- 危険性のある機能追加に備え、権限管理と監査可能性を持たせる

## 3. スコープ

本仕様の対象:

- プラグインの配置方式
- ロードと初期化の流れ
- プラグインが提供するコマンド定義方式
- 権限管理
- 監査ログ
- UI 連携方式
- マクロ保存互換

本仕様の対象外:

- 個別プラグインの実装詳細
- 個別機能の処理アルゴリズム
- 特定ベンダー SDK や外部ライブラリの詳細

## 4. 想定するプラグインの役割

プラグインは、`AutoTool` 本体に含めない追加機能を提供する単位として扱います。

役割の例:

- 外部システム連携
- デバイス連携
- 画像処理や判定処理
- 表示支援
- ファイル入出力
- 独自コマンド追加

ここで重要なのは個々の機能詳細ではなく、どの種類の拡張でも同じ枠組みで追加・管理できることです。

## 5. 全体構成

推奨する構成は以下です。

- `AutoTool.Plugin.Abstractions`
  - プラグイン契約、権限定義、共通 DTO を格納
- `AutoTool.Plugin.Host`
  - プラグイン探索、検証、ロード、権限管理、監査連携
- `Plugins\<PluginId>\`
  - プラグイン実体を配置するディレクトリ
  - `plugin.json`
  - 実装 DLL
  - 依存ファイル

## 6. レイヤ責務

### 6.1 Bootstrap / Desktop

- プラグインフォルダの探索
- プラグイン一覧の表示
- プラグイン権限承認 UI
- プラグイン由来の表示要求の受け渡し
- 実行ログ画面へのプラグイン識別表示

### 6.2 Application

- プラグイン設定保存のユースケース
- 監査ログ保存のユースケース
- 権限承認状態の読書き
- プラグイン管理画面向けの読み取りモデル

### 6.3 Domain

- プラグイン権限、承認状態、監査イベントなどの業務ルール
- 危険操作に必要な承認条件

### 6.4 Automation.Runtime

- プラグイン由来コマンド定義の登録
- マクロのシリアライズ互換維持
- プラグイン未導入時の未解決コマンド表現

### 6.5 Infrastructure

- DLL ロード
- 署名検証
- ハッシュ検証
- ファイルシステムアクセス
- プラグインが必要とする技術実装の接続

## 7. プラグイン配布形式

各プラグインは `Plugins\<PluginId>\` 配下へ配置します。

例:

```text
Plugins\
  Example.Plugin\
    plugin.json
    Example.Plugin.dll
    dependencies\
```

### 7.1 `plugin.json`

最低限の必須項目は以下とします。

```json
{
  "pluginId": "Example.Plugin",
  "displayName": "Example Plugin",
  "version": "1.0.0",
  "entryAssembly": "Example.Plugin.dll",
  "entryType": "Example.Plugin.PluginEntry",
  "minHostVersion": "1.1.0",
  "permissions": [
    "external.read",
    "external.write"
  ],
  "commands": [
    {
      "commandType": "Example.Plugin.Command",
      "displayName": "Example Command",
      "category": "System",
      "order": 10,
      "showInCommandList": true
    }
  ],
  "quickActions": [
    {
      "actionId": "open-console",
      "displayName": "コンソール",
      "commandType": "Example.Plugin.Command",
      "toolTip": "コンソールを開きます",
      "icon": "PanelRight24",
      "order": 10,
      "location": "ExtensionToolbar",
      "parameterJson": {}
    }
  ],
  "signatureThumbprint": "0123456789ABCDEF"
}
```

### 7.2 配置例

リポジトリには次の 2 種類の例を置きます。

- `PluginSamples\\Sample.Plugin\\plugin.json`
  - 読込・表示・実行確認を目的にした最小サンプル
- `PluginTemplates\\Template.Plugin\\plugin.json`
  - 改名して育てる前提のひな形

`Publish-SamplePlugin.ps1` でサンプル配置を、`Publish-TemplatePlugin.ps1` でテンプレート配置を試せるようにします。

### 7.3 項目定義

- `pluginId`
  - 一意な識別子
  - マクロ保存時の参照キーとして使用
- `displayName`
  - UI 表示名
- `version`
  - プラグイン自身のバージョン
- `entryAssembly`
  - エントリ DLL 名
- `entryType`
  - `IAutoToolPlugin` 実装型の完全修飾名
- `minHostVersion`
  - 最低対応ホストバージョン
- `permissions`
  - 要求権限一覧
- `commands`
  - プラグインが公開するコマンド定義一覧
  - `showInCommandList` はコマンド追加画面へ表示するかどうかを指定する。未指定時は `true`
  - 同じ `commandType` をプラグイン本体の `IPluginCommandDefinitionProvider` でも返す場合、詳細定義は本体側を使用し、`showInCommandList` は `plugin.json` 側の指定を優先する
- `quickActions`
  - 拡張ツールバーへ常設表示する固定引数ショートカット定義一覧
  - `actionId` / `displayName` / `commandType` は必須
  - `location` は Phase 1 では `ExtensionToolbar` のみ
  - `icon` は `Wpf.Ui.Controls.SymbolRegular` の enum 名を指定し、不正な値は既定アイコンへフォールバックする
- `signatureThumbprint`
  - 許可済み証明書の指紋

## 8. プラグイン契約

`AutoTool.Plugin.Abstractions` に以下の契約を定義します。

### 8.1 `IAutoToolPlugin`

役割:

- プラグインメタ情報の提供
- 初期化
- 終了処理
- `entryAssembly` / `entryType` からの実インスタンス化対象

想定メンバー:

```csharp
public interface IAutoToolPlugin
{
    PluginDescriptor Descriptor { get; }
    ValueTask InitializeAsync(IPluginInitializationContext context, CancellationToken cancellationToken);
    ValueTask DisposeAsync(CancellationToken cancellationToken);
}
```

### 8.2 `IPluginCommandDefinitionProvider`

役割:

- 追加コマンド定義の提供
- 既存の `ReflectionCommandRegistry` に統合できる形でメタデータを返す

### 8.3 `IPluginServiceRegistrar`

役割:

- プラグインが必要とする依存関係を明示登録する
- Service Locator を避け、コンストラクタ DI を維持する
- ホスト起動時に `IServiceCollection` へ反映するための登録情報を返す

### 8.4 `IPluginCommandExecutor`

役割:

- プラグインコマンドの実行本体を提供する
- `IPluginExecutionContext` を通してホスト機能を利用する
- プラグイン固有パラメータは `PluginCommandExecutionRequest` から受け取る

### 8.5 `IPluginHealthCheck`

役割:

- 起動時にプラグインの利用可否を確認する
- ホストの起動時診断へ状態を返す

### 8.6 映像ストリーム契約

`AutoTool.Plugin.Abstractions.Video` に、カメラプラグインなどが提供する映像ストリームを他プラグインから参照するための契約を定義します。

提供側プラグインは `Mitaka.Camera` などの個別実装を他プラグインへ参照させず、初期化時に `IPluginInitializationContext.VideoStreams` から `IVideoStreamRegistry.RegisterAsync(...)` を呼び出します。
利用側プラグインは `Mitaka.Camera` へ project reference / assembly reference を持たず、`IPluginServiceRegistrar` で登録したサービスのコンストラクタに `IVideoStreamRegistry` を受け取り、`GetSources()` で一覧を見てから `GetSourceAsync(sourceId, ...)` で `IVideoFrameSource` を取得します。

提供側の最小例:

```csharp
public async ValueTask<PluginInitializationResult> InitializeAsync(
    IPluginInitializationContext context,
    CancellationToken cancellationToken)
{
    await context.VideoStreams.RegisterAsync(new VideoStreamRegistration
    {
        SourceId = "mitaka.camera.main",
        DisplayName = "メインカメラ",
        ProviderPluginId = "Mitaka.Camera",
        Width = 1920,
        Height = 1080,
        PixelFormat = VideoPixelFormat.Bgr24,
        Source = new CameraVideoFrameSource("mitaka.camera.main"),
    }, cancellationToken);

    return PluginInitializationResult.Success();
}
```

利用側の最小例:

```csharp
public sealed class ImageProcessingService(IVideoStreamRegistry videoStreams)
{
    public async ValueTask RunAsync(string sourceId, CancellationToken cancellationToken)
    {
        var source = await videoStreams.GetSourceAsync(sourceId, cancellationToken);
        if (source is null)
        {
            return;
        }

        await foreach (var frame in source.GetFramesAsync(null, cancellationToken))
        {
            using (frame)
            {
                // frame.ImageData は ReadOnlyMemory<byte> のため、不要な byte[] コピーを避けて処理できます。
            }
        }
    }
}
```

`VideoFrame` は画像本体を `ReadOnlyMemory<byte>` として保持します。カメラ SDK などが寿命管理を必要とするバッファを返す場合は、`VideoFrame` の `owner` に `IDisposable` を渡し、利用側が `VideoFrame.Dispose()` することで解放できます。

同じ `SourceId` が複数登録された場合、registry は重複として登録を拒否します。重複や登録済み映像ソース数は起動時診断の `IPluginStartupDiagnosticsCatalog` から確認できます。

既存の `Mitaka.Camera` 側に仮置きした `VideoFrame` / `IVideoFrameSource` / `IVideoStreamRegistry` 相当の型がある場合は、次の方針で差し替えます。

- 仮置きモデルを `AutoTool.Plugin.Abstractions.Video` の正式型へ置換する
- `Mitaka.Camera` は `IPluginInitializationContext.VideoStreams.RegisterAsync(...)` で `IVideoFrameSource` を登録する
- 画像処理プラグインは `Mitaka.Camera` を参照せず、`IVideoStreamRegistry` から `SourceId` 指定で取得する

### 8.7 `IPluginExecutionContext`

役割:

- コマンド実行時に必要なホスト機能を抽象で提供する
- UI 技術や外部実装詳細を直接参照させずに、ホストへ要求を委譲する

想定メンバー:

```csharp
public interface IPluginExecutionContext
{
    DateTimeOffset GetLocalNow();
    void Log(string message);
    void ReportProgress(int progress);
    string? GetVariable(string name);
    void SetVariable(string name, string value);
    string ResolvePath(string path);
    ValueTask PublishAsync(PluginUiRequest request, CancellationToken cancellationToken);
    ValueTask WriteArtifactAsync(PluginArtifactRequest request, CancellationToken cancellationToken);
}
```

`ResolvePath(...)` と `WriteArtifactAsync(...)` の相対パスは、現在開いている `.macro` ファイルのフォルダを基準に解決されます。未保存または未読込でマクロファイルの場所がない場合は、ホストアプリの配置フォルダを基準にします。

## 9. コマンド定義方式

基本方針は、既存のコマンド定義方式を壊さずに、プラグイン由来の定義を追加登録できるようにすることです。

- 本体標準コマンド
  - 既存どおり `[CommandDefinition]` ベース
- プラグインコマンド
  - プラグインロード後に定義をレジストリへ追加
  - `pluginId` を持つ
  - `showInCommandList` が `false` の場合を除き、UI 上は通常コマンドと同様に表示する
  - 実行時はホスト側の `PluginCommand` から対象プラグインへ委譲する
  - `properties` が定義されている場合は編集 UI を動的生成する

必要な識別情報:

- `CommandType`
- `DisplayName`
- `Category`
- `EditorSchema`
- `pluginId`
- `requiredPermissions`
- `version`

現在の実装では、`plugin.json` と `IPluginCommandDefinitionProvider` の両方から定義を集約し、`IPluginCommandExecutor` を実装したプラグインへ実行を委譲します。
また、`properties` に定義された項目は `ParameterJson` と相互変換され、編集画面では構造化された入力欄として表示されます。
さらに、`IPluginServiceRegistrar` が返したサービス登録はホスト起動時にアプリケーション DI へ反映されます。
加えて、`IPluginHealthCheck` 実装がある場合は起動時に実行され、権限定義の整合確認と合わせて `IPluginStartupDiagnosticsCatalog` から参照できます。

`quickActions` は、既存のプラグインコマンド実行経路を再利用する固定引数ショートカットです。
ホストは対象プラグインの `commandType` と照合し、拡張ツールバーのボタン押下時に `PluginCommandListItem` を組み立てて `PluginCommandDispatcher` へ渡します。
専用の別実行基盤は持たず、`parameterJson` が未指定の場合は `{}` として実行します。

## 10. マクロ保存互換

`.macro` にはプラグイン由来コマンドであることを保持します。

保存項目の例:

- `type`
- `pluginId`
- `pluginVersionRange`
- `properties`

読込時の扱い:

- 対応プラグインあり
  - 通常どおり読込・編集・実行可能
- 対応プラグインなし
  - 未解決コマンドとして表示
  - 元データは保持し、再保存で壊さない
- バージョン不一致
  - 警告表示
  - 互換範囲内なら読込許可
  - 互換外なら編集・実行を制限

## 11. 権限モデル

権限はプラグイン単位とコマンド単位の二段階で扱います。

### 11.1 権限の考え方

- プラグインは必要最小限の権限のみ要求する
- 危険性の高い操作は明示的な権限へ分離する
- 権限未承認の機能は実行不可とする
- 権限名は機能分類ベースで整理し、個別実装名に依存させない

### 11.2 運用ルール

- 初回ロード時に要求権限一覧を表示する
- 重要権限を含む場合は明示承認を必須とする
- 未承認権限を必要とするコマンドは実行不可とする
- 権限変更を含む更新時は再承認できるようにする
- 起動時診断では、コマンドが要求する権限が `permissions` に宣言されているかを確認する

## 12. 監査ログ

プラグイン利用時は、本体ログとは別に監査観点の記録を残します。

記録項目:

- `timestamp`
- `operator`
- `pluginId`
- `commandType`
- `target`
- `requestedPermissions`
- `result`
- `errorCode`
- `errorMessage`

最低限の要件:

- 実行開始と終了を記録する
- 失敗時は原因分類を残す
- プラグイン識別子とコマンド種別を必ず残す

## 13. UI 仕様

### 13.1 プラグイン管理画面

表示項目:

- 有効 / 無効
- `pluginId`
- 表示名
- バージョン
- 署名検証結果
- 依存ファイル状態
- 権限一覧
- 最終ヘルスチェック結果

操作:

- 有効化 / 無効化
- 権限承認
- 再スキャン
- 状態確認

### 13.2 実行中表示

- 実行ログに `pluginId` を表示する
- プラグインからの UI 要求はホスト側の専用領域で処理する
- 表示処理そのものはホストが責任を持つ
- 起動時診断の結果はステータスメッセージとログパネルへ表示できるようにする

### 13.3 拡張ツールバー

- `quickActions.location` が `ExtensionToolbar` の項目を専用の拡張ツールバーへ表示する
- 標準操作群とは分離し、title bar には追加しない
- AutoTool 実行中は quick action ボタンを無効化する
- コマンド未提供など利用不可の場合は、無効理由を tooltip で確認できるようにする
- ボタン押下時は `QuickAction 実行: <DisplayName>` をログへ記録する

## 14. セキュリティと配布

- 署名付きプラグインのみロードできるようにする
- 許可フォルダ配下のみ読込対象とする
- `plugin.json` と実体 DLL の整合を検証する
- `minHostVersion` 不一致なら無効化する
- ロード失敗時でもアプリ全体は起動継続できるようにする

## 15. 導入フェーズ

### Phase 1

- プラグイン探索
- `plugin.json` 読込
- 署名検証
- 権限表示
- コマンド定義追加
- 未解決コマンド保持

現時点の実装着手順:

- `AutoTool.Plugin.Abstractions`
  - プラグイン契約と基本モデル
- `AutoTool.Plugin.Host`
  - `plugin.json` の読込
  - 必須項目検証
  - プラグインディレクトリ探索
  - `commands` からのコマンド定義抽出
  - `entryAssembly` / `entryType` による DLL 読込
- `ReflectionCommandRegistry`
  - 外部コマンド定義メタデータの取り込み

### Phase 2

- 監査ログ保存
- プラグイン管理画面の整備
- 実行コンテキスト拡張

現時点の起動時診断:

- `IPluginHealthCheck` 実装がある場合は起動時に実行する
- コマンド定義が要求する権限一覧を集約する
- `commands.requiredPermissions` が `permissions` に含まれていない場合は異常として扱う
- 診断結果は `IPluginStartupDiagnosticsCatalog` から参照できる
- Desktop 側では診断結果を整形し、ステータスバーとログパネルへ表示する

### Phase 3

- UI 要求の標準化
- エラー診断情報の整備
- 更新時の再承認フロー整備

## 16. 実装時の注意

- プラグインへ WPF 依存を直接持たせない
- Service Locator を使わず、明示的 DI を維持する
- プラグイン未導入時の `.macro` 再保存互換を優先して守る
- プラグインの表示要求と表示実装を分離する
- プラグイン依存ファイルはプラグインフォルダ内で完結しやすくする

## 17. 次の実装タスク

本仕様に基づく最初の実装タスクは以下です。

1. `AutoTool.Plugin.Abstractions` を追加する
2. `plugin.json` のモデルとバリデータを追加する
3. プラグイン探索・ロード・無効化のホスト機能を追加する
4. コマンドレジストリへプラグイン定義を追加できる拡張点を用意する
5. 未解決コマンドの保存互換を追加する
6. プラグイン管理画面の最小 UI を追加する








