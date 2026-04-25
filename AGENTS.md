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

- ビルド完了後、実行に必要な成果物（`exe` / `dll` / 依存ランタイム等）をデプロイ先ディレクトリに配置する。
- ビルド完了後、成果物は毎回、運用で定めた指定の配布先ディレクトリへコピーする。
- 指定の配布先ディレクトリは `C:\AutoTool` とし、ビルド完了後は毎回ここへ成果物をコピーする。
- デプロイは `deploy-to-c-autotool.ps1` を正規手順とし、手動コピーは行わない。
- デプロイ系処理（publish → copy → 検証）は並列実行せず、必ず直列実行する。
- `deploy-to-c-autotool.ps1` は既定で publish を実行して `.deploy\AutoTool_publish` を最新化してからコピーする。
- `-SkipPublish` は配布元が最新であることを確認できる場合のみ使用する。
- コピー対象は実行に必要な成果物のみに限定し、過不足がないことを確認する。
- 設定ファイルはデプロイ先の `Settings` 配下（例: `Settings\appsettings.json`）に配置する。
- デプロイ時はユーザーデータ領域と設定領域を除外保護し、既存データを削除しない。
- 初回デプロイ時のみ、設定ファイルが存在しない場合に既定設定を投入する。
- 新しい成果物に置き換わって不要になった設定ファイル・ランタイム・関連ファイルは、デプロイ先から削除する。
- 配置先の内容は都度メンテナンスし、古い成果物を残置しない。
- フレームワーク依存配布（対象 RID はプロジェクト方針に従う）を前提とし、不要なデバッグ成果物（`*.pdb`）と import library（`*.lib`）は配置しない。既存配置にある場合は削除する。
- ロケール配下ディレクトリ（例: `cs` / `de` / `fr` など）は空ディレクトリを残さない。空であることを確認して削除する。
- 64bit 運用時は `x86` 配下を配置しない。既存配置に `x86` が残っている場合は削除する。
- 配置後は最低限、メイン実行ファイルの存在、設定ファイルの存在、ユーザーデータ保持件数を確認する。
- 配置後は `AutoTool.exe` / `AutoTool.dll` / `AutoTool.Desktop.dll` のハッシュ一致を確認し、不一致は失敗として扱う。

## Architecture Policy (Clean Architecture / DDD)

- 新規実装・大きな改修では、Clean Architecture と DDD の原則を優先する。
- 依存関係は内側（Domain）に向ける。外側（UI / Infrastructure）から内側へは依存してよいが、内側から外側への依存は禁止する。
- ドメイン層には業務ルールと不変条件のみを置き、永続化・UI・外部サービスの詳細を持ち込まない。
- ユースケース（Application 層）は「何をするか」を記述し、「どう保存するか」「どう通信するか」は抽象（interface）越しに扱う。
- Infrastructure 層は技術詳細（DB、API、ファイル、メッセージング等）の実装責務を持ち、ドメイン知識を持たせない。
- エンティティ・値オブジェクトは用語（ユビキタス言語）を反映した命名を行い、プリミティブ値の乱用を避ける。
- ビジネス上重要なルールはドメインモデルで表現し、Application 層や UI 側に分散させない。
- Aggregate 境界を意識し、整合性が必要な更新は Aggregate 単位で完結させる。
- Repository は Aggregate 永続化のための抽象として定義し、クエリ最適化は必要に応じて読み取りモデルを分離する。
- テストは Domain / Application を優先し、インフラ詳細に依存しない形で主要ユースケースの回帰を防ぐ。
- 既存構造との整合性を尊重し、一括変更ではなく段階的に適用する。
- `Application` / `Domain` から `System.Windows` / `Microsoft.Win32` / `AutoTool.Desktop` への参照を持ち込まない。
- `DllImport` / `LibraryImport` は `Infrastructure` に限定し、他層で直接利用しない。
- WPF 固有の UI アダプタ（ダイアログ、ファイルピッカー、Dispatcher など）は `Desktop` に配置し、`Infrastructure` へ残さない。

## Dependency Injection Policy

- `IServiceProvider` の直接利用や `GetService(...)` 呼び出しによる Service Locator パターンは採用しない。
- 依存はコンストラクタインジェクション（DI）で明示的に受け取る。
- やむを得ず遅延解決が必要な場合も、専用ファクトリ/抽象を介して依存関係を明示し、呼び出し側にロケータを露出させない。

## Documentation Sync Policy

- 実装変更・仕様変更・挙動変更を行った場合は、必要に応じて関連する `.md` ドキュメント（`README.md` / `docs/*.md` / 運用ドキュメント）を同一変更内で更新する。
- 少なくとも、ユーザーや開発者の判断に影響する差分（手順、設定、コマンド仕様、制約、配布手順）はドキュメントへ反映する。
- ドキュメント更新が不要と判断した場合は、その理由を PR / コミットメッセージ等で明示する。
- 新規ドキュメント追加時も既存ドキュメントとの導線（相互リンク）を維持し、重複・矛盾を残さない。
- 実装変更・仕様変更・挙動変更が配布利用者向け手順やコマンド説明に影響する場合は、配布同梱の `Readme.txt` / `Readme_コマンド詳細.txt` も同一変更内で更新する。
- バージョンアップや GitHub Release 更新を行う場合は、`README.md` の `最近の更新` を対象バージョンの実際の変更内容に合わせて更新する。

## Language Policy (Japanese)

- AGENTS.md を含む運用ドキュメント、ユーザー向けメッセージ、ログ文言は原則日本語で記述する。
- 内部識別子・外部仕様・プロトコル・ライブラリAPI名など、英語固定が必要な箇所は例外として保持する。

## Encoding Policy

- テキストファイルは UTF-8 BOM 付きで保存する。
- AGENTS.md を含む運用ドキュメントも UTF-8 BOM を維持する。
