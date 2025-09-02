using AutoTool.Model.List.Interface;
using AutoToolICommand = AutoTool.Command.Interface.ICommand;

namespace AutoTool.Message
{
    /// <summary>
    /// Phase 5完全統合版：メッセージクラス
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    
    // Undo/Redo関連メッセージ
    public class UndoMessage { }
    public class RedoMessage { }

    // ファイル操作メッセージ
    public class SaveMessage 
    {
        public string? FilePath { get; }
        public SaveMessage(string? filePath = null) => FilePath = filePath;
    }

    public class LoadMessage 
    {
        public string? FilePath { get; }
        public LoadMessage(string? filePath = null) => FilePath = filePath;
    }

    // リスト操作メッセージ
    public class AddMessage 
    {
        public string ItemType { get; }
        public AddMessage(string itemType) => ItemType = itemType;
    }

    public class DeleteMessage { }
    public class ClearMessage { }

    public class UpMessage { }
    public class DownMessage { }

    // 実行制御メッセージ
    public class RunMessage { }
    public class StopMessage { }

    // 選択変更メッセージ
    public class ChangeSelectedMessage 
    {
        public ICommandListItem? Item { get; }
        public ChangeSelectedMessage(ICommandListItem? item) => Item = item;
    }

    // 編集メッセージ
    public class EditCommandMessage 
    {
        public ICommandListItem Item { get; }
        public EditCommandMessage(ICommandListItem item) => Item = item;
    }

    public class RefreshListViewMessage { }

    // ログメッセージ
    public class LogMessage 
    {
        public string Text { get; }
        public LogMessage(string text) => Text = text;
    }

    // Phase 5追加：コマンド実行関連メッセージ
    public class StartCommandMessage 
    {
        public AutoToolICommand Command { get; }
        public StartCommandMessage(AutoToolICommand command) => Command = command;
    }

    public class FinishCommandMessage 
    {
        public AutoToolICommand Command { get; }
        public FinishCommandMessage(AutoToolICommand command) => Command = command;
    }

    public class DoingCommandMessage 
    {
        public AutoToolICommand Command { get; }
        public string Detail { get; }
        public DoingCommandMessage(AutoToolICommand command, string detail) 
        {
            Command = command;
            Detail = detail;
        }
    }

    public class UpdateProgressMessage 
    {
        public AutoToolICommand Command { get; }
        public int Progress { get; }
        public UpdateProgressMessage(AutoToolICommand command, int progress) 
        {
            Command = command;
            Progress = progress;
        }
    }
}