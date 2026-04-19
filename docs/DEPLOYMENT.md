# 配布ガイド

## 1. 目的

このドキュメントは、AutoTool の成果物を配布先へ安全に配置する手順を示します。

## 2. 基本方針

- 配布はフレームワーク依存配布を前提にする
- 設定は `Settings\appsettings.json` に配置する
- ユーザーデータ（`Macro`）と既存設定は保護する
- 不要成果物（`*.pdb`、`*.lib`、`x86` など）は削除する

## 3. 配置スクリプト実行（推奨）

`deploy-to-c-autotool.ps1` は既定で以下を順番に実行します。

- `AutoTool.Bootstrap` を `Release` で publish（`.deploy\AutoTool_publish`）
- `C:\AutoTool` へ同期コピー（`Macro` / `Settings` は保護）
- 主要バイナリ（`AutoTool.exe` / `AutoTool.dll` / `AutoTool.Desktop.dll`）のハッシュ一致確認

```powershell
.\deploy-to-c-autotool.ps1 -Destination C:\AutoTool
```

必要に応じて publish 対象や構成を指定できます。

```powershell
.\deploy-to-c-autotool.ps1 -Project .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj -Configuration Release -Source .\.deploy\AutoTool_publish -Destination C:\AutoTool
```

すでに最新 publish 済みで、コピーだけ実行したい場合:

```powershell
.\deploy-to-c-autotool.ps1 -SkipPublish -Source .\.deploy\AutoTool_publish -Destination C:\AutoTool
```

## 4. 手動で publish する場合

例:

```powershell
dotnet publish .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj -c Release -o .\.deploy\AutoTool_publish
```

## 5. スクリプトの主な処理

- `robocopy /MIR` で成果物を同期（`Macro` と `Settings` は除外）
- 初回のみ `Settings\appsettings.json` を投入
- `*.pdb` / `*.lib` を削除
- `x86` ディレクトリを削除
- 空のロケールディレクトリを削除
- メイン EXE / 設定ファイル / ユーザーデータ件数を検証
- 主要バイナリのハッシュ一致を検証（未一致時は上書き再試行）

## 6. 配置後チェック

- 配布先にメイン EXE がある
- 配布先に `Settings\appsettings.json` がある
- `Macro` 配下の既存データ件数が保持されている

## 7. 失敗時の確認

- publish 出力フォルダが最新か（必要に応じて `-SkipPublish` を外す）
- 配布先ディレクトリ権限があるか
- `robocopy` の終了コードが 8 以上になっていないか
- 実行中の `AutoTool.exe` がファイルをロックしていないか

## 8. 運用メモ

- 配布前に `dotnet build` / `dotnet test` を実行してから公開する
- 配布手順は毎回同じスクリプトで実施し、手作業差分をなくす

## 9. GitHub で ZIP 配布する場合

`SR1CTRL` と同様に GitHub Releases で ZIP 配布する場合は、`.github/workflows/release-zip.yml` を利用します。

- トリガー
  - `v*` タグ push（例: `v1.0.0`）
  - 手動実行（`workflow_dispatch`）
- 実行内容
  - `AutoTool.Bootstrap` を `win-x64` で publish
  - 配布対象（`AutoTool.exe`、`*.dll`、`Readme*.txt`、実行構成ファイル）を収集
  - ZIP（`AutoTool-<version>-win-x64.zip`）を生成
  - タグ実行時は GitHub Release へ ZIP を自動添付

### タグ配布の例

```powershell
git tag v1.0.0
git push origin v1.0.0
```

### 注意

- ZIP には `*.pdb` / `*.lib` を含めない。
- 配布版説明書（`Readme.txt` / `Readme_コマンド詳細.txt`）はリポジトリ root に置き、ZIP へ同梱する。
