# 利用者向けガイド

## 1. このアプリでできること

AutoTool は、繰り返し作業を「マクロ」として保存し、再実行するためのツールです。

- クリック、キー入力、待機などの操作自動化
  - クリックの押下維持時間（ms）を設定可能
  - 注入方式を `MouseEvent` / `SendInput` から選択可能
  - クリック前マウス移動シミュレートを ON/OFF 可能
- 画像検索（OpenCV）によるターゲット検出
- OCR（Tesseract）による文字判定
- AI 検出（ONNX Runtime）による対象検出

## 2. はじめに

### 2.1 配布版（ZIP）を使う場合

- ZIP を任意フォルダへ展開し、`AutoTool.exe` を起動します
- 起動できない場合は `.NET 10 Desktop Runtime` をインストールしてください
- 詳細は同梱 `Readme.txt` を参照してください

### 2.2 ソースから実行する場合

前提:

- Windows
- .NET 10 SDK

実行:

```powershell
dotnet restore .\AutoTool.sln
dotnet build .\AutoTool.sln -c Debug
dotnet run --project .\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj
```

## 3. 画面の見方

メイン画面は主に次の領域で構成されます。

- タイトルバー: 開く、保存、Undo/Redo、設定、バージョン情報
- マクロ編集エリア: コマンド一覧の編集
- ログパネル: 実行ログ確認
- 実行前チェックパネル: 実行前の設定不備確認
- お気に入りパネル: テンプレート再利用
- ステータスバー: 実行状態・メッセージ表示

## 4. 基本操作フロー

1. コマンドを追加して手順を作る
2. 必要なパラメータを編集する
3. `.macro` ファイルとして保存する
4. 実行前チェック結果（要修正/問題なし）を確認する
5. 実行してログで結果を確認する

## 5. 主なショートカット

- `Ctrl+O`: 開く
- `Ctrl+S`: 上書き保存
- `Ctrl+Shift+S`: 名前を付けて保存
- `Ctrl+Z`: 元に戻す
- `Ctrl+Y` / `Ctrl+Shift+Z`: やり直し
- `Ctrl+C`: 選択中コマンドをコピー
- `Ctrl+V`: 選択行の下へ貼り付け
- `Ctrl+X`: 切り取り
- `Alt+↑` / `Alt+↓`: 選択行を上下に移動
- `Ctrl+A` → `Delete`: コマンド一覧を全削除
- `Shift+クリック` / `Ctrl+クリック`: 複数選択
- `Esc` を約 1.2 秒長押し: 緊急停止（非アクティブ時も有効）

## 6. 実行前チェックとテスト機能

- 実行時に画像パス・`tessdata`・外部実行パスを一括チェック
- 要修正がある場合は実行を開始せず、エラーコード付きで表示
- 右下の `診断` ボタンでチェックパネルを開閉可能

編集パネルの単発テスト:

- OCR系コマンド: `OCRプレビュー` / `自動調整`
- 画像検索系コマンド: `画像検索テスト` / `自動調整`
- AI検出系コマンド: `AI検出テスト` / `自動調整`

補足:

- 検出成功時は対象範囲を赤枠で点滅表示
- `自動調整` の採用値は編集表示だけでなくコマンド設定に即時反映（保存対象）

## 7. 設定・保存先

- マクロファイル: `.macro`
- 設定ファイル: `Settings\appsettings.json`
- 画面状態: `Settings\window_settings.json`
- 最近使ったファイル: `Settings\RecentFiles_*.xml`
- お気に入り: `Settings\favorites.xml`

## 8. バージョン表示と配布版の扱い

- タイトルバーの `バージョン情報` でバージョンと GitHub URL を確認できます
- URL はクリックしてブラウザで開けます
- 正式バージョンの正は GitHub タグ（`vMAJOR.MINOR.PATCH`）です
- `deploy-to-c-autotool.ps1` で配布した場合、表示バージョンは最新タグ由来になります

## 9. よくあるトラブル

- 起動しない
  - 配布版: `.NET 10 Desktop Runtime` の有無を確認
  - ソース実行: `dotnet --info` と `dotnet build` のエラーを確認
- OCR が動かない / 精度が低い
  - `tessdata` ディレクトリを確認
  - `jpn+eng取得` または手動で `jpn.traineddata` / `eng.traineddata` を配置
- 保存できない
  - 書き込み先フォルダの権限を確認

## 10. 関連資料

- 配布版説明: [Readme.txt](../Readme.txt)
- コマンド詳細: [Readme_コマンド詳細.txt](../Readme_コマンド詳細.txt)
- 開発者向け: [開発者向けガイド](DEVELOPER_GUIDE.md)
- 配布運用: [配布ガイド](DEPLOYMENT.md)
