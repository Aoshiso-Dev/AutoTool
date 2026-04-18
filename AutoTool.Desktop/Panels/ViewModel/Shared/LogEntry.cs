namespace AutoTool.Desktop.Panels.ViewModel.Shared;

public enum LogEntryLevel
{
    Info,
    Start,
    Success,
    Warning,
    Error
}

public sealed class LogEntry
{
    public required string Timestamp { get; init; }
    public required string LineNumber { get; init; }
    public required string CommandName { get; init; }
    public required string Message { get; init; }
    public required LogEntryLevel Level { get; init; }

    public string SearchableText => $"{Timestamp} {LineNumber} {CommandName} {Message}";
}

