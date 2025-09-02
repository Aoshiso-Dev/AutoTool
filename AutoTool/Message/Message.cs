using AutoTool.Model.List.Interface;
using AutoToolICommand = AutoTool.Command.Interface.ICommand;

namespace AutoTool.Message
{
    /// <summary>
    /// Phase 5���S�����ŁF���b�Z�[�W�N���X
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    
    // Undo/Redo�֘A���b�Z�[�W
    public class UndoMessage { }
    public class RedoMessage { }

    // �t�@�C�����상�b�Z�[�W
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

    // ���X�g���상�b�Z�[�W
    public class AddMessage 
    {
        public string ItemType { get; }
        public AddMessage(string itemType) => ItemType = itemType;
    }

    public class DeleteMessage { }
    public class ClearMessage { }

    public class UpMessage { }
    public class DownMessage { }

    // ���s���䃁�b�Z�[�W
    public class RunMessage { }
    public class StopMessage { }

    // �I��ύX���b�Z�[�W
    public class ChangeSelectedMessage 
    {
        public ICommandListItem? Item { get; }
        public ChangeSelectedMessage(ICommandListItem? item) => Item = item;
    }

    // �ҏW���b�Z�[�W
    public class EditCommandMessage 
    {
        public ICommandListItem Item { get; }
        public EditCommandMessage(ICommandListItem item) => Item = item;
    }

    public class RefreshListViewMessage { }

    // ���O���b�Z�[�W
    public class LogMessage 
    {
        public string Text { get; }
        public LogMessage(string text) => Text = text;
    }

    // Phase 5�ǉ��F�R�}���h���s�֘A���b�Z�[�W
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