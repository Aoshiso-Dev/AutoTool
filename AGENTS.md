# AGENTS

## C# Language Policy (Prefer Latest)

- 新規実装・リファクタでは、可読性を損なわない範囲で、より新しい C# 記法・機能を優先して採用する。
- 既存コードとの整合性と互換性を保ちつつ、段階的に新しい記法へ移行する。

### Preferred Syntax

- Null チェックは `ArgumentNullException.ThrowIfNull(...)` を優先する。
- 適用可能な箇所では `primary constructor` を使用する。
- 型推論が明確な場合は `new()` を使用する。
- 使える場面では collection expression (`[]`) を使用する。
- 条件分岐が簡潔になる場合は `switch expression` を使用する。
- パターンマッチ (`is`, `is not`, `and/or`, 再帰パターン) を積極的に使用する。
- `using` は file-scoped namespace / using declaration など簡潔な記法を優先する。
- データ表現には `required` / `init` / `record` を優先する。

### Async / Time / Logging Conventions

- 不要な `Task.Run(async () => ...)` は避け、直接 `await` を優先する。
- 時刻は `TimeProvider` + `DateTimeOffset` を基本とし、`DateTime.Now` の新規利用は避ける。
- ログは構造化ログを優先する。

### Safety / Compatibility

- 変更時は TargetFramework / LangVersion / 依存ライブラリとの整合性を確認する。
- 可読性や互換性を下げる変更は行わない。
- 変更後はビルド確認を行い、回帰がないことを確認する。

## Build Output Deployment Policy

- ビルド完了後、実行に必要な `exe` / `dll` / 依存ランタイム等を `C:\AutoTool` にコピーして配置する。
- コピー対象は、実行に必要な成果物のみとし、過不足がないことを確認する。
- 設定ファイルは `C:\AutoTool\Settings\appsettings.json` に配置する。
- デプロイ時は `Macro` と `Settings` を除外保護し、ユーザーデータ/設定を削除しない。
- 初回デプロイ時のみ、`Settings\appsettings.json` が存在しない場合に公開成果物の既定設定を投入する。
- 新しい成果物に置き換わって不要になった設定ファイル・ランタイム・関連ファイルは、`C:\AutoTool` から削除する。
- 配置先の内容は都度メンテナンスし、古い成果物を残置しない。
- フレームワーク依存配布（win-x64）を前提とし、不要なデバッグ成果物（`*.pdb`）と import library（`*.lib`）は配置しない。既存配置にある場合は削除する。
- ロケール配下ディレクトリ（例: `cs` / `de` / `fr` など）は空ディレクトリを残さない。空であることを確認して削除する。
- 64bit 運用時は `x86` 配下を配置しない。既存配置に `x86` が残っている場合は削除する。
- 配置後は最低限 `AutoTool.exe` の存在、`Settings\appsettings.json` の存在、`Macro` の保持件数を確認する。

## Encoding Policy

- テキストファイルは UTF-8 BOM 付きで保存する。
- AGENTS.md を含む運用ドキュメントも UTF-8 BOM を維持する。
