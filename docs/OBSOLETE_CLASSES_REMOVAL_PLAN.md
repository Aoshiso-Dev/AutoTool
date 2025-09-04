# AutoTool 廃止予定クラス削除計画

## 現在廃止予定のクラス

### 1. `Panels\ViewModel\EditPanelViewModel`
- **状態**: `[Obsolete("Phase 3で統合版に移行。AutoTool.ViewModel.Panels.EditPanelViewModelを使用してください", false)]`
- **削除予定**: v2.0.0
- **代替クラス**: `AutoTool.ViewModel.Panels.EditPanelViewModel`
- **影響範囲**: MacroPanelsプロジェクト全体
- **削除手順**:
  1. 全参照を新しいクラスに置き換え
  2. テストケースの更新
  3. ドキュメント更新
  4. クラス削除

### 2. `Panels\ViewModel\ButtonPanelViewModel`
- **状態**: `[Obsolete("Phase 3で統合版に移行。AutoTool.ViewModel.Panels.ButtonPanelViewModelを使用してください", false)]`
- **削除予定**: v2.0.0
- **代替クラス**: `AutoTool.ViewModel.Panels.ButtonPanelViewModel`
- **影響範囲**: UI統合処理
- **削除手順**: EditPanelViewModelと同様

## 削除スケジュール

### Phase 1: 警告期間 (現在 - v1.9.0)
- `[Obsolete]`属性でコンパイル時警告
- ドキュメントで移行ガイド提供
- 新機能は統合版のみに追加

### Phase 2: エラー化 (v1.9.0 - v2.0.0)
```csharp
[Obsolete("このクラスは削除されました。AutoTool.ViewModel.Panels.EditPanelViewModelを使用してください", true)]
```

### Phase 3: 完全削除 (v2.0.0)
- 廃止クラスの物理削除
- 関連テストケースの削除
- プロジェクトファイルからの除去

## 自動化スクリプト

```powershell
# 廃止クラス検出スクリプト
Get-ChildItem -Recurse -Filter "*.cs" | Select-String "\[Obsolete" | ForEach-Object {
    Write-Host "廃止予定: $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())"
}

# 参照検索スクリプト  
$obsoleteClasses = @("MacroPanels.ViewModel.EditPanelViewModel", "MacroPanels.ViewModel.ButtonPanelViewModel")
foreach ($class in $obsoleteClasses) {
    Get-ChildItem -Recurse -Filter "*.cs" | Select-String $class | ForEach-Object {
        Write-Host "要更新: $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())"
    }
}
```

## 移行チェックリスト

- [ ] 全`using`文の更新
- [ ] DIコンテナ登録の更新  
- [ ] XAMLバインディングの確認
- [ ] 単体テストの更新
- [ ] 結合テストの実行
- [ ] パフォーマンステストの実行
- [ ] ドキュメントの更新
- [ ] CHANGELOG.mdの更新

## リスク評価

### 高リスク
- MainWindowViewModelの統合処理
- 複雑なプロキシプロパティの移行

### 中リスク  
- XAMLバインディングの更新漏れ
- DIコンテナ設定の不整合

### 低リスク
- ログ出力メッセージの変更
- コメントや文字列リテラル

## 緊急時ロールバック手順

1. Gitタグ作成（削除前）
2. バックアップブランチ作成
3. 問題発生時の即座戻し手順書作成
4. 顧客通知テンプレート準備

## 削除完了後の効果

### コード品質向上
- 重複コード除去: 約800行削減
- 保守性向上: 統一されたアーキテクチャ
- テスト容易性: DI対応による高いテスタビリティ

### パフォーマンス向上
- メモリ使用量削減: 重複クラス除去
- 初期化時間短縮: オブジェクト生成の最適化
- 実行速度向上: プロキシ層の削減