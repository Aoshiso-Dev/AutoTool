using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.Messages;

/// <summary>
/// グローバルログメッセージ（アプリ全体でログを収集するため維持）
/// </summary>
public class LogMessage
{
    public string Text { get; }
    public LogMessage(string text) => Text = text;
}

// 以下のメッセージクラスは廃止され、直接イベントに置き換えられました:
// - 実行/停止/保存/読み込みメッセージ
// - 全消去/追加/上へ/下へ/削除メッセージ
// - 選択変更/編集/一覧更新メッセージ

