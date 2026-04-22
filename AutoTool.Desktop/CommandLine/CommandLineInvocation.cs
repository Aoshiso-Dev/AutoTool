namespace AutoTool.Desktop.CommandLine;

/// <summary>
/// コマンドライン経由で要求された起動・制御内容を保持します。
/// </summary>
public sealed record CommandLineInvocation(
    string? MacroPath,
    bool Start,
    bool Stop,
    bool Exit,
    bool ExitOnComplete,
    bool Hide,
    bool Show,
    bool SilentErrors)
{
    public static CommandLineInvocation Empty { get; } = new(
        MacroPath: null,
        Start: false,
        Stop: false,
        Exit: false,
        ExitOnComplete: false,
        Hide: false,
        Show: false,
        SilentErrors: false);

    public bool HasAnyOperation =>
        !string.IsNullOrWhiteSpace(MacroPath)
        || Start
        || Stop
        || Exit
        || ExitOnComplete
        || Hide
        || Show;

    public bool ShouldStartHidden => Hide && !Show;
}
