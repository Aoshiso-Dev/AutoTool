using AutoTool.Commands.Interface;

namespace AutoTool.Commands.Message;

/// <summary>
/// コマンド実行中メッセージのイベント引数です。
/// </summary>
public class DoingCommandEventArgs : EventArgs
{
    /// <summary>詳細メッセージです。</summary>
    public string Detail { get; set; }
    public DoingCommandEventArgs(string detail) { Detail = detail; }
}

/// <summary>
/// コマンド開始通知メッセージです。
/// </summary>
public class StartCommandMessage
{
    public ICommand Command { get; set; }
    public StartCommandMessage(ICommand command) { Command = command; }
}

/// <summary>
/// コマンド終了通知メッセージです。
/// </summary>
public class FinishCommandMessage
{
    public ICommand Command { get; set; }
    public FinishCommandMessage(ICommand command) { Command = command; }
}

/// <summary>
/// コマンド進行中の詳細通知メッセージです。
/// </summary>
public class DoingCommandMessage
{
    public ICommand Command { get; set; }
    public string Detail { get; set; }
    public DoingCommandMessage(ICommand command, string detail) { Command = command; Detail = detail; }
}

/// <summary>
/// 進捗更新通知メッセージです。
/// </summary>
public class  UpdateProgressMessage
{
    public ICommand Command { get; set; }
    public int Progress { get; set; }
    public UpdateProgressMessage(ICommand command, int progress) { Command = command;  Progress = progress; }
}
