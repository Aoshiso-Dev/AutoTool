## サンプルプラグインの確認

- PluginSamples\\Sample.Plugin\\plugin.json に最小構成の配置例があります。
- Publish-SamplePlugin.ps1 はサンプルプラグインをビルドし、Plugins\\Sample.Plugin 形式へ配置します。
- 既定の配置先は .\\.deploy\\AutoTool_publish\\Plugins\\Sample.Plugin です。
- ローカル実行で確認する場合は -Destination .\\AutoTool.Desktop\\bin\\Release\\net10.0-windows\\Plugins\\Sample.Plugin を指定します。
- 配置後に AutoTool を再起動すると、コマンド一覧へ Provider Command が表示され、対象変数 と 設定値 を編集して実行できます。

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
  - 画面、ViewModel、Host 構成、WPF 固有アダプタ、起動時診断表示
- `AutoTool.Application`
  - ユースケース、履歴管理、Port 抽象
- `AutoTool.Domain`
  - ドメインモデルと不変条件
- `AutoTool.Automation.Contracts`
  - 実行契約・サービス抽象
- `AutoTool.Plugin.Abstractions`
  - プラグイン契約・基本モデル
- `AutoTool.Plugin.Template`
  - 改名して育てる前提のプラグインひな形
- `AutoTool.Plugin.Host`
  - プラグイン探索・`plugin.json` 読込・検証・DLL 読込・起動時診断・サービス登録反映・実行委譲・動的プロパティ解決
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

補足:

- `deploy-to-c-autotool.ps1` の本体同期は `Plugins` を保護します。
- publish 出力に `Plugins` がある場合は、その内容だけを別同期して追加・更新します。

## 8. ドキュメント更新チェック

- 実装/仕様/挙動を変更した
- 配布手順、制約、コマンド説明が変わった
- 画面表示文言、利用者判断に関わる内容が変わった

上記に該当する場合は次を同一変更で更新する:

- `README.md`
- `docs/*.md`
- `Readme.txt`
- `Readme_コマンド詳細.txt`

## 9. プラグインひな形

- 実行確認用の最小サンプルは `AutoTool.Tests.Plugin.Sample` と `PluginSamples\\Sample.Plugin` にあります。
- 改名して育てる前提のひな形は `AutoTool.Plugin.Template` と `PluginTemplates\\Template.Plugin` にあります。
- 一時配置で確認する場合は `Publish-TemplatePlugin.ps1` を使います。既定配置先は `.\\.tmp\\Template.Plugin` です。

## 10. 関連ドキュメント

- [アーキテクチャ概要](ARCHITECTURE.md)
- [プラグインアーキテクチャ仕様](PLUGIN_ARCHITECTURE.md)
- [クラス図（主要関係）](CLASS_DIAGRAM.md)
- [配布ガイド](DEPLOYMENT.md)
- [コマンド開発ガイド](COMMAND_DEVELOPMENT_GUIDE.md)
- [利用者向けガイド](USER_GUIDE.md)









