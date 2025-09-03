using AutoTool.Command.Interface;
using AutoTool.Model.List.Interface;
using System;

namespace AutoTool.Message
{
    // ��{�R�}���h���b�Z�[�W
    public class RunMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class StopMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class PauseMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class ResumeMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    // ���X�g���상�b�Z�[�W
    public class AddMessage
    {
        public string ItemType { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public AddMessage(string itemType)
        {
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
        }
    }

    public class DeleteMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class UpMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class DownMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class ClearMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    // �t�@�C�����상�b�Z�[�W
    public class SaveMessage
    {
        public string? FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public SaveMessage(string? filePath = null)
        {
            FilePath = filePath;
        }
    }

    public class LoadMessage
    {
        public string? FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public LoadMessage(string? filePath = null)
        {
            FilePath = filePath;
        }
    }

    // �V�����t�@�C�����상�b�Z�[�W�iMainWindow�p�j
    public class LoadFileMessage
    {
        public string FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public LoadFileMessage(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
    }

    public class SaveFileMessage
    {
        public string FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public SaveFileMessage(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
    }

    // Undo/Redo ���b�Z�[�W
    public class UndoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class RedoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    // ��ԕύX���b�Z�[�W
    public class ChangeSelectedMessage
    {
        public ICommandListItem? SelectedItem { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public ChangeSelectedMessage(ICommandListItem? selectedItem)
        {
            SelectedItem = selectedItem;
        }
    }

    // �A�C�e�����ύX���b�Z�[�W
    public class ItemCountChangedMessage
    {
        public int Count { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public ItemCountChangedMessage(int count)
        {
            Count = count;
        }
    }

    // �}�N�����s��ԃ��b�Z�[�W
    public class MacroExecutionStateMessage
    {
        public bool IsRunning { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public MacroExecutionStateMessage(bool isRunning)
        {
            IsRunning = isRunning;
        }
    }

    // �R�}���h���s�֘A���b�Z�[�W�i�C���Łj
    public class StartCommandMessage
    {
        public ICommand Command { get; }
        public int LineNumber { get; }
        public string ItemType { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public StartCommandMessage(ICommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            LineNumber = command.LineNumber;
            ItemType = GetItemTypeFromCommand(command);
        }

        /// <summary>
        /// �R�}���h����ItemType�𐳊m�Ɏ擾
        /// </summary>
        private static string GetItemTypeFromCommand(ICommand command)
        {
            return command.GetType().Name switch
            {
                "WaitImageCommand" => "Wait_Image",
                "ClickImageCommand" => "Click_Image", 
                "ClickImageAICommand" => "Click_Image_AI",
                "HotkeyCommand" => "Hotkey",
                "ClickCommand" => "Click",
                "WaitCommand" => "Wait",
                "LoopCommand" => "Loop",
                "LoopBreakCommand" => "Loop_Break",
                "LoopEndCommand" => "Loop_End",
                "IfImageExistCommand" => "IF_ImageExist",
                "IfImageNotExistCommand" => "IF_ImageNotExist",
                "IfImageExistAICommand" => "IF_ImageExist_AI",
                "IfImageNotExistAICommand" => "IF_ImageNotExist_AI",
                "IfVariableCommand" => "IF_Variable",
                "IfEndCommand" => "IF_End",
                "ExecuteCommand" => "Execute",
                "SetVariableCommand" => "SetVariable",
                "SetVariableAICommand" => "SetVariable_AI",
                "ScreenshotCommand" => "Screenshot",
                _ => command.GetType().Name.Replace("Command", "")
            };
        }
    }

    public class FinishCommandMessage
    {
        public ICommand Command { get; }
        public int LineNumber { get; }
        public string ItemType { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public FinishCommandMessage(ICommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            LineNumber = command.LineNumber;
            ItemType = GetItemTypeFromCommand(command);
        }

        /// <summary>
        /// �R�}���h����ItemType�𐳊m�Ɏ擾
        /// </summary>
        private static string GetItemTypeFromCommand(ICommand command)
        {
            return command.GetType().Name switch
            {
                "WaitImageCommand" => "Wait_Image",
                "ClickImageCommand" => "Click_Image", 
                "ClickImageAICommand" => "Click_Image_AI",
                "HotkeyCommand" => "Hotkey",
                "ClickCommand" => "Click",
                "WaitCommand" => "Wait",
                "LoopCommand" => "Loop",
                "LoopBreakCommand" => "Loop_Break",
                "LoopEndCommand" => "Loop_End",
                "IfImageExistCommand" => "IF_ImageExist",
                "IfImageNotExistCommand" => "IF_ImageNotExist",
                "IfImageExistAICommand" => "IF_ImageExist_AI",
                "IfImageNotExistAICommand" => "IF_ImageNotExist_AI",
                "IfVariableCommand" => "IF_Variable",
                "IfEndCommand" => "IF_End",
                "ExecuteCommand" => "Execute",
                "SetVariableCommand" => "SetVariable",
                "SetVariableAICommand" => "SetVariable_AI",
                "ScreenshotCommand" => "Screenshot",
                _ => command.GetType().Name.Replace("Command", "")
            };
        }
    }

    public class DoingCommandMessage
    {
        public ICommand Command { get; }
        public string Detail { get; }
        public int LineNumber { get; }
        public string ItemType { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public DoingCommandMessage(ICommand command, string detail)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Detail = detail ?? string.Empty;
            LineNumber = command.LineNumber;
            ItemType = GetItemTypeFromCommand(command);
        }

        /// <summary>
        /// �R�}���h����ItemType�𐳊m�Ɏ擾
        /// </summary>
        private static string GetItemTypeFromCommand(ICommand command)
        {
            return command.GetType().Name switch
            {
                "WaitImageCommand" => "Wait_Image",
                "ClickImageCommand" => "Click_Image", 
                "ClickImageAICommand" => "Click_Image_AI",
                "HotkeyCommand" => "Hotkey",
                "ClickCommand" => "Click",
                "WaitCommand" => "Wait",
                "LoopCommand" => "Loop",
                "LoopBreakCommand" => "Loop_Break",
                "LoopEndCommand" => "Loop_End",
                "IfImageExistCommand" => "IF_ImageExist",
                "IfImageNotExistCommand" => "IF_ImageNotExist",
                "IfImageExistAICommand" => "IF_ImageExist_AI",
                "IfImageNotExistAICommand" => "IF_ImageNotExist_AI",
                "IfVariableCommand" => "IF_Variable",
                "IfEndCommand" => "IF_End",
                "ExecuteCommand" => "Execute",
                "SetVariableCommand" => "SetVariable",
                "SetVariableAICommand" => "SetVariable_AI",
                "ScreenshotCommand" => "Screenshot",
                _ => command.GetType().Name.Replace("Command", "")
            };
        }
    }

    // �R�}���h�G���[���b�Z�[�W
    public class CommandErrorMessage
    {
        public ICommand Command { get; }
        public Exception Exception { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CommandErrorMessage(ICommand command, Exception exception)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    /// <summary>
    /// �i���X�V���b�Z�[�W�i�C���Łj
    /// </summary>
    public class UpdateProgressMessage
    {
        public ICommand Command { get; }
        public int Progress { get; }
        public int LineNumber { get; }
        public string ItemType { get; }        
        public DateTime Timestamp { get; } = DateTime.Now;

        public UpdateProgressMessage(ICommand command, int progress)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Progress = Math.Max(0, Math.Min(100, progress));
            LineNumber = command.LineNumber;
            ItemType = GetItemTypeFromCommand(command);
        }

        /// <summary>
        /// �R�}���h����ItemType�𐳊m�Ɏ擾
        /// </summary>
        private static string GetItemTypeFromCommand(ICommand command)
        {
            return command.GetType().Name switch
            {
                "WaitImageCommand" => "Wait_Image",
                "ClickImageCommand" => "Click_Image", 
                "ClickImageAICommand" => "Click_Image_AI",
                "HotkeyCommand" => "Hotkey",
                "ClickCommand" => "Click",
                "WaitCommand" => "Wait",
                "LoopCommand" => "Loop",
                "LoopBreakCommand" => "Loop_Break",
                "LoopEndCommand" => "Loop_End",
                "IfImageExistCommand" => "IF_ImageExist",
                "IfImageNotExistCommand" => "IF_ImageNotExist",
                "IfImageExistAICommand" => "IF_ImageExist_AI",
                "IfImageNotExistAICommand" => "IF_ImageNotExist_AI",
                "IfVariableCommand" => "IF_Variable",
                "IfEndCommand" => "IF_End",
                "ExecuteCommand" => "Execute",
                "SetVariableCommand" => "SetVariable",
                "SetVariableAICommand" => "SetVariable_AI",
                "ScreenshotCommand" => "Screenshot",
                _ => command.GetType().Name.Replace("Command", "")
            };
        }
    }

    // �ϐ��ύX���b�Z�[�W
    public class VariableChangedMessage
    {
        public string Name { get; }
        public string Value { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public VariableChangedMessage(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? string.Empty;
        }
    }

    // �ϐ��N���A���b�Z�[�W
    public class VariablesClearedMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    // �R�}���h���s���v���b�Z�[�W
    public class CommandStatsMessage
    {
        public int TotalCommands { get; }
        public int ExecutedCommands { get; }
        public int SuccessfulCommands { get; }
        public int FailedCommands { get; }
        public TimeSpan ExecutionTime { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CommandStatsMessage(int total, int executed, int successful, int failed, TimeSpan executionTime)
        {
            TotalCommands = total;
            ExecutedCommands = executed;
            SuccessfulCommands = successful;
            FailedCommands = failed;
            ExecutionTime = executionTime;
        }
    }

    // ���؃��b�Z�[�W
    public class ValidationMessage
    {
        public string Type { get; }
        public string Message { get; }
        public bool IsError { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public ValidationMessage(string type, string message, bool isError = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            IsError = isError;
        }
    }

    // ���O���b�Z�[�W
    public class LogMessage
    {
        public string Level { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public LogMessage(string level, string message, Exception? exception = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
        }
    }

    // �X�e�[�^�X�X�V���b�Z�[�W
    public class StatusUpdateMessage
    {
        public string Status { get; }
        public string? Details { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public StatusUpdateMessage(string status, string? details = null)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Details = details;
        }
    }

    // �A�C�e���^�C�v�ύX���b�Z�[�W
    public class ChangeItemTypeMessage
    {
        public ICommandListItem OldItem { get; }
        public ICommandListItem NewItem { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public ChangeItemTypeMessage(ICommandListItem oldItem, ICommandListItem newItem)
        {
            OldItem = oldItem ?? throw new ArgumentNullException(nameof(oldItem));
            NewItem = newItem ?? throw new ArgumentNullException(nameof(newItem));
        }
    }

    // ���X�g�r���[�X�V���b�Z�[�W
    public class RefreshListViewMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}