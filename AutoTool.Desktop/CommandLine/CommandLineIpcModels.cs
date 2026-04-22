namespace AutoTool.Desktop.CommandLine;

/// <summary>
/// IPC 経由で送受信するリクエストです。
/// </summary>
public sealed record CommandLineIpcRequest(CommandLineInvocation Invocation);

/// <summary>
/// IPC 経由で返す結果です。
/// </summary>
public sealed record CommandLineIpcResponse(int ExitCode, string Message)
{
    public static CommandLineIpcResponse Ok(string message = "OK") => new(0, message);
}

/// <summary>
/// 起動制御で利用する終了コードです。
/// </summary>
public static class CommandLineExitCodes
{
    public const int Success = 0;
    public const int RuntimeFailure = 1;
    public const int InvalidArguments = 2;
    public const int TargetNotFound = 3;
}
