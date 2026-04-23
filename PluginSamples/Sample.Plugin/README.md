# Sample.Plugin

`Sample.Plugin` は、AutoTool のプラグイン読込、コマンド表示、編集 UI、実行委譲をまとめて確認するための最小サンプルです。

## 含まれるコマンド

- `Provider Command`
  - `対象変数` に指定した変数へ、`設定値` を代入します。

## 配置方法

1. `Publish-SamplePlugin.ps1` を実行して、配置先の `Plugins\Sample.Plugin` を作成します。
2. AutoTool を起動または再起動します。
3. コマンド一覧に `Provider Command` が表示されることを確認します。
4. コマンドを追加し、`対象変数` と `設定値` を編集して実行します。

## 例

配布用 publish へ配置する場合:

```powershell
.\Publish-SamplePlugin.ps1
```

ローカルの Desktop 実行フォルダへ配置する場合:

```powershell
.\Publish-SamplePlugin.ps1 -Destination .\AutoTool.Desktop\bin\Release\net10.0-windows\Plugins\Sample.Plugin
```
