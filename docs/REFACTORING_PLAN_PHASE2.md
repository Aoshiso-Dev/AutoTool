# Phase2 リファクタリング計画（Domain/Application への再分割）

## 目的

- 現在 `AutoTool.Automation.Runtime` に集約されている責務を、段階的に `AutoTool.Domain` / `AutoTool.Application` へ再配置する。
- 各PRで必ずビルドを通し、動作回帰を最小化する。

## 前提

- ブランチ: `codex/reorg-namespace-architecture`
- 現在の構成はビルド成功済み
- 1PRごとに `dotnet build .\AutoTool.sln -c Debug` を必須実行

## PR1: Domain の最小純化（依存の薄い型から） ✅ 完了

### 対象

- `AutoTool.Automation.Runtime\Model\FavoriteMacroEntry.cs`
- `AutoTool.Automation.Runtime\Model\Panels\List\Type\ConditionType.cs`
- `AutoTool.Automation.Runtime\Model\Panels\List\Interface\ICommandList.cs`

### 結果

- `FavoriteMacroEntry` を `AutoTool.Domain.Macros` へ移動
- `ConditionType` を `AutoTool.Domain.Automation.Conditions` へ移動
- `ICommandList` を `AutoTool.Domain.Automation.Lists` へ移動

## PR2: Application Port の分離 ✅ 完了

### 対象

- `ICapturePathProvider`
- `IFavoriteMacroStore`
- `IFilePicker`
- `ILogWriter`
- `IPanelDialogService`
- `IRecentFileStore`
- `IStatusMessageScheduler`

### 結果

- `AutoTool.Application.Ports` へ移動
- `RecentFileEntry` を新設し、`IRecentFileStore` の Runtime 依存を解消

## PR3: Application UseCase（履歴管理）分離 ✅ 完了

### 対象

- `CommandHistoryManager`
- `IUndoRedoCommand`
- `HistoryCommands/*`

### 結果

- `AutoTool.Application.History` / `AutoTool.Application.History.Commands` へ移動
- Desktop/Test 側参照を追従済み

## PR4: Namespace 正規化（段階実施）

### PR4-1: Runtime 周辺の legacy namespace 整理 ✅ 完了

- `AutoTool.Panels.Model.CommandDefinition` → `AutoTool.Automation.Runtime.Definitions`
- `AutoTool.Panels.Model.MacroFactory` → `AutoTool.Automation.Runtime.MacroFactory`
- `AutoTool.Panels.List.Class` → `AutoTool.Automation.Runtime.Lists`
- `AutoTool.Panels.Serialization` → `AutoTool.Automation.Runtime.Serialization`
- `AutoTool.Panels.Attributes` → `AutoTool.Automation.Runtime.Attributes`
- `AutoTool.Panels.Message` → `AutoTool.Automation.Runtime.Messages`

### PR4-2: Desktop Panels namespace 整理 ✅ 完了

- `AutoTool.Panels.*` → `AutoTool.Desktop.Panels.*`
- XAML の `x:Class` / `clr-namespace` も追従済み

### PR4-3: 契約/Infrastructure namespace 正規化 ✅ 完了

- `AutoTool.Panels.Model.List.Interface` → `AutoTool.Automation.Contracts.Lists`
- `AutoTool.Panels.Helpers` → `AutoTool.Infrastructure.Paths`
- `AutoTool.Panels.Services` → `AutoTool.Infrastructure.Panels`
- ファイル配置も namespace に合わせて移動済み

## PR5: Desktop/Application 最終整理 ✅ 完了

### PR5-1: FileManager の Application 層移行 ✅ 完了

- `FileManager` を `AutoTool.Model` から `AutoTool.Application.Files` へ移行
- 参照側の `using` / static import を `AutoTool.Application.Files.FileManager` に追従

### PR5-2: Desktop ルート namespace 統一 ✅ 完了

- `AutoTool.View*` / `AutoTool.ViewModel` / `AutoTool.Hosting` / `AutoTool.Model`
  を `AutoTool.Desktop.*` に統一
- `MainWindow` / `MainWindowViewModel` を `AutoTool.Desktop` へ整理
- XAML (`x:Class` / `clr-namespace`) と `App.xaml` の `x:Class` も整合済み

## PR6: DI ポリシー準拠の強化 ✅ 完了

### PR6-1: Service Locator 残存の削減 ✅ 完了

- `GetService(...)` の利用を撤去
- `IServiceProvider` 直接利用を排除
- `ICommandDependencyResolver` に型ベース実装 (`CommandDependencyResolver`) を追加
- `PanelsHostBuilder` / Benchmark / Application Test の DI 登録を `sp => ...` 依存から整理

### PR6-2: 依存解決責務の分離とテスト補強 ✅ 完了

- `CommandFactory` から依存解決インターフェース/実装を分離し、`CommandDependencyResolver.cs` を新設
- `CommandDependencyResolver` の単体テストを追加
  - 既知依存解決
  - `TimeProvider` の既定解決
  - 未知型解決失敗

### PR6-3: Application 回帰テスト拡充 ✅ 完了

- `History.Commands` の回帰テストを追加
  - `AddItemCommand` / `RemoveItemCommand` / `MoveItemCommand` / `EditItemCommand` / `ClearAllCommand`
- `CommandHistoryManager` の Undo/Redo 遷移（Undo後の新規実行で Redo クリア）を追加
- `FileManager` の最近使ったファイル上限（10件維持）を追加

### PR6-4: Domain 実ケーステスト追加 ✅ 完了

- `ConditionType` の契約テストを追加
  - 期待列挙値と順序
  - 重複/空値なし
- `FavoriteMacroEntry` のモデルテストを追加
  - 既定値
  - 値保持

### PR6-5: Domain 不変条件の型内集約 ✅ 完了

- `ConditionType` に妥当性APIを追加
  - `IsSupported(string?)`
  - `TryParse(string?, out string)`
- `FavoriteMacroEntry` にモデル規約APIを追加
  - `Create(...)`（必須値検証 + 正規化）
  - `Normalize()`
  - `IsValid()`
- `FavoritePanelViewModel` 読込/追加時に正規化・妥当性フィルタを適用
- `MacroPanelViewModel` のお気に入り作成を `FavoriteMacroEntry.Create(...)` に統一

### PR6-6: モダン化と依存解決最適化 ✅ 完了

- `ConditionType` を `static class` 化し、定義セットを内部キャッシュ化
- `FavoriteMacroEntry` を `record` 化（既存シリアライズ互換を維持）
- `CommandDependencyResolver` を解決マップ方式へ最適化
  - exact type は辞書で即時解決
  - assignable type はフォールバック探索
- Domain テストに `record` 等価性ケースを追加

### PR6-7: `required` 段階導入検証 ✅ 完了

- `FavoriteMacroEntry` に `required` を導入
  - `Name`
  - `SnapshotPath`
- XMLシリアライザ互換のため、`[SetsRequiredMembers]` 付き parameterless ctor を併用
- Domain/Application/全体ビルドで互換性確認済み

### PR6-8: `required` 横展開（Application モデル）✅ 完了

- `RecentFileEntry` に `required` を導入
  - `FileName`
  - `FilePath`
- 既存シリアライズ/逆シリアライズ互換のため、`[SetsRequiredMembers]` 付き parameterless ctor を併用
- `RecentFileEntry` モデルテストを追加

### PR6-9: `required` 横展開（File 設定モデル）✅ 完了

- `FileManager.FileTypeInfo` に `required` を導入
  - `Filter`
  - `DefaultExt`
  - `Title`
- 互換のため、`[SetsRequiredMembers]` 付き parameterless ctor を併用
- `FileTypeInfo` モデルテストを追加

### PR6-10: DI 登録の全体共通化 ✅ 完了

- マクロ実行系の共通DI登録を `AddMacroRuntimeCoreServices()` に集約
  - `ICommandDependencyResolver`
  - `ICommandFactory`
  - `IMacroFactory`
  - `ReflectionCommandRegistry` / `ICommandRegistry` / `ICommandDefinitionProvider`
  - `IMacroFileSerializer` / `CommandList`
- 適用先
  - `PanelsHostBuilder`
  - `MacroFactoryBenchmarks`
  - `CommandListAndMacroFactoryTests`
- 登録重複を削減し、今後の変更点を1箇所化

### PR6-11: Resolver 構成の用途分離 ✅ 完了

- `CommandDependencyResolver` を用途別 Resolver の合成へリファクタ
  - `CoreCommandDependencyResolver`（入出力/画像/OCRなどの実行依存）
  - `AmbientCommandDependencyResolver`（`ICommandEventBus` / `TimeProvider`）
  - `CompositeCommandDependencyResolver`（解決チェーン）
- 外部公開契約 (`ICommandDependencyResolver`) は維持しつつ内部責務を分離
- `CommandDependencyResolverTests` に `ICommandEventBus` 解決ケースを追加

### PR6-12: DI ポリシー回帰ガードの自動化 ✅ 完了

- `DependencyInjectionPolicyTests` を追加し、主要プロダクションコード配下を走査して Service Locator パターン混入を検知
  - `IServiceProvider`
  - `GetService(...)`
  - `GetRequiredService(...)`
- 旧 namespace 混入の回帰も検知
  - `AutoTool.Core`
  - `AutoTool.Commands.Abstractions`
  - `AutoTool.Panels`
  - `AutoTool.ViewModel` / `AutoTool.View.*` / `AutoTool.Hosting`
- 対象は `AutoTool.Application` / `AutoTool.Automation.Contracts` / `AutoTool.Automation.Runtime` / `AutoTool.Bootstrap` / `AutoTool.Desktop` / `AutoTool.Domain` / `AutoTool.Infrastructure`
- `bin` / `obj` / 生成コードは除外して誤検知を抑制

## 全体ステータス（PR6時点）

- Domain/Application の責務分離・namespace 正規化・DI是正・回帰テスト拡充は完了
- DI ポリシー違反の回帰検知をテストで自動化済み
- `required` の段階導入は主要モデルで完了（`FavoriteMacroEntry` / `RecentFileEntry` / `FileTypeInfo`）
- 現在の回帰状況:
  - `AutoTool.Tests.Domain`: 22 pass
  - `AutoTool.Tests.Application`: 35 pass
  - `AutoTool.Tests.Infrastructure`: 31 pass
  - `AutoTool.Tests.Desktop`: 4 pass
  - `dotnet build AutoTool.sln -c Release`: 成功

## 次フェーズ（PR6+ 継続）

- 依存解決まわりの用途別 Resolver 分割（必要時）
- 主要モデルへの `required` 適用候補洗い出し（残り）

## 検証チェックリスト（各PR共通）

1. `dotnet restore .\AutoTool.sln`
2. `dotnet build .\AutoTool.sln -c Debug`
3. `dotnet test .\AutoTool.Tests.Application\AutoTool.Tests.Application.csproj -c Debug`
4. `dotnet test .\AutoTool.Tests.Domain\AutoTool.Tests.Domain.csproj -c Debug`
5. `dotnet test .\AutoTool.Tests.Infrastructure\AutoTool.Tests.Infrastructure.csproj -c Debug`
6. `dotnet test .\AutoTool.Tests.Desktop\AutoTool.Tests.Desktop.csproj -c Debug`
7. アプリ起動確認（`AutoTool.Bootstrap`）
