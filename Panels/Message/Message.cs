using MacroPanels.Model.List.Interface;

namespace MacroPanels.Message;

/// <summary>
/// グローバルログメッセージ（アプリ全体でログを収集するため維持）
/// </summary>
public class LogMessage
{
    public string Text { get; }
    public LogMessage(string text) => Text = text;
}

// 以下のメッセージクラスは廃止され、直接イベントに置き換えられました:
// - RunMessage, StopMessage, SaveMessage, LoadMessage
// - ClearMessage, AddMessage, UpMessage, DownMessage, DeleteMessage
// - ChangeSelectedMessage, EditCommandMessage, RefreshListViewMessage
