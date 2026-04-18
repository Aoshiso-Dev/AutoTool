# 配布ガイド

## 1. 目的

このドキュメントは、AutoTool の成果物を配布先へ安全に配置する手順を示します。

## 2. 基本方針

- 配布はフレームワーク依存配布を前提にする
- 設定は `Settings\appsettings.json` に配置する
- ユーザーデータ（`Macro`）と既存設定は保護する
- 不要成果物（`*.pdb`、`*.lib`、`x86` など）は削除する

## 3. 発行（publish）

例:

```powershell
dotnet publish .\AutoTool\AutoTool.csproj -c Release -r win-x64 --self-contained false -o .\.deploy\AutoTool_publish
```

## 4. 配置スクリプト実行

`deploy-to-c-autotool.ps1` を使って配布先を更新します。

```powershell
.\deploy-to-c-autotool.ps1 -Source .\.deploy\AutoTool_publish -Destination C:\AutoTool
```

スクリプトの主な処理:

- `robocopy /MIR` で成果物を同期（`Macro` と `Settings` は除外）
- 初回のみ `Settings\appsettings.json` を投入
- `*.pdb` / `*.lib` を削除
- `x86` ディレクトリを削除
- 空のロケールディレクトリを削除
- メイン EXE / 設定ファイル / ユーザーデータ件数を検証

## 5. 配置後チェック

- 配布先にメイン EXE がある
- 配布先に `Settings\appsettings.json` がある
- `Macro` 配下の既存データ件数が保持されている

## 6. 失敗時の確認

- publish 出力フォルダが存在するか
- 配布先ディレクトリ権限があるか
- `robocopy` の終了コードが 8 以上になっていないか

## 7. 運用メモ

- 配布前に `dotnet build` / `dotnet test` を実行してから公開する
- 配布手順は毎回同じスクリプトで実施し、手作業差分をなくす