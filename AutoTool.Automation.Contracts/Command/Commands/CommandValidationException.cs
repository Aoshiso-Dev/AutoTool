namespace AutoTool.Commands.Commands;

/// <summary>
/// 不正な入力や実行時エラーの詳細を保持して呼び出し元へ通知します。
/// </summary>
public sealed class CommandValidationException : Exception
{
    public int LineNumber { get; }
    public string CommandName { get; }
    public string ErrorCode { get; }
    public string PropertyName { get; }

    public CommandValidationException(
        int lineNumber,
        string commandName,
        string errorCode,
        string propertyName,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LineNumber = lineNumber;
        CommandName = commandName;
        ErrorCode = errorCode;
        PropertyName = propertyName;
    }
}
