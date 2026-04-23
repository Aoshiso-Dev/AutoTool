# Template.Plugin

`Template.Plugin` は、AutoTool プラグインを新規作成するときのひな形です。

このテンプレートは次の点を最小構成で含みます。

- `IAutoToolPlugin`
- `IPluginCommandDefinitionProvider`
- `IPluginCommandExecutor`
- `IPluginHealthCheck`
- `plugin.json`
- 編集 UI に表示される `properties` 定義

## 使い始め方

1. `AutoTool.Plugin.Template` プロジェクトを複製し、プロジェクト名と namespace を目的の名前へ変更します。
2. `TemplatePluginConstants.cs` の `PluginId`、`EntryAssembly`、`EntryType`、`CommandType` を変更します。
3. このフォルダの `plugin.json` を、DLL 名と完全修飾型名に合わせて更新します。
4. `GetCommandDefinitions()` の `DisplayName`、`Description`、`Properties` を実装内容に合わせて変更します。
5. `ExecuteCommandAsync()` を目的の処理へ置き換えます。
6. 必要に応じて `permissions`、`IPluginServiceRegistrar`、追加コマンドを実装します。

## 最初に置き換える場所

- `TemplatePluginConstants.cs`
- `TemplatePlugin.cs`
- `plugin.json`

## 動作確認用のコマンド

初期状態では `Write Variable` コマンドが 1 つあり、指定した変数へ値を設定します。
テンプレートのままでもロード・編集・実行の確認に使えます。

## 配置のしかた

一時配置で確認する場合:

```powershell
.\Publish-TemplatePlugin.ps1
```

配置先を明示する場合:

```powershell
.\Publish-TemplatePlugin.ps1 -Destination .\AutoTool.Desktop\bin\Release\net10.0-windows\Plugins\Template.Plugin
```
