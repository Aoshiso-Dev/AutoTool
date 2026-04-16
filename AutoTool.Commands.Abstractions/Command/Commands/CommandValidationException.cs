namespace AutoTool.Commands.Commands;

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
