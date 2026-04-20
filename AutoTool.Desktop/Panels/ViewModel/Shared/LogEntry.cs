namespace AutoTool.Desktop.Panels.ViewModel.Shared;

/// <summary>
/// ログエントリの表示レベルです。
/// </summary>
public enum LogEntryLevel
{
    Info,
    Start,
    Success,
    Warning,
    Error
}

/// <summary>
/// ログ表示用の1行データです。
/// </summary>
public sealed class LogEntry
{
    public required string Timestamp { get; init; }
    public required string LineNumber { get; init; }
    public required string CommandName { get; init; }
    public required string Message { get; init; }
    public required LogEntryLevel Level { get; init; }

    public string SearchableText => $"{Timestamp} {LineNumber} {CommandName} {Message}";
}

