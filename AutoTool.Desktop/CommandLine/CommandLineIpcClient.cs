using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AutoTool.Desktop.CommandLine;

/// <summary>
/// 既存インスタンスへコマンドライン要求を転送します。
/// </summary>
public static class CommandLineIpcClient
{
    public const string PipeName = "AutoTool.CommandLine.v1";

    public static async Task<CommandLineIpcResponse?> SendAsync(
        CommandLineInvocation invocation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        await using var client = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await client.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("既存インスタンスへの接続がタイムアウトしました。");
        }

        var requestJson = JsonSerializer.Serialize(new CommandLineIpcRequest(invocation));
        await using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        await writer.WriteLineAsync(requestJson.AsMemory(), timeoutCts.Token).ConfigureAwait(false);
        var responseJson = await reader.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CommandLineIpcResponse>(responseJson);
    }
}
