using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.AccessControl;
using System.Security.Principal;
using AutoTool.Application.Ports;
using AutoTool.Desktop.CommandLine;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Hosting;

/// <summary>
/// 既存インスタンスに対するコマンドライン要求を受信し、UI へ中継します。
/// </summary>
public sealed class CommandLineIpcHostedService(
    CommandLineControlService controlService,
    ILogWriter logWriter) : IHostedService, IDisposable
{
    private readonly CommandLineControlService _controlService = EnsureNotNull(controlService);
    private readonly ILogWriter _logWriter = EnsureNotNull(logWriter);
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = Task.Run(() => RunServerLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_serverTask is not null)
        {
            try
            {
                await _serverTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task RunServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = NamedPipeServerStreamAcl.Create(
                    CommandLineIpcClient.PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    CreatePipeSecurity());

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                await using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

                var requestJson = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                _logWriter.Write("INFO", "CommandLineIpc", $"Received={requestJson}");
                CommandLineIpcResponse response;
                try
                {
                    response = await HandleRequestAsync(requestJson, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logWriter.Write(ex);
                    response = new CommandLineIpcResponse(
                        CommandLineExitCodes.RuntimeFailure,
                        $"IPC 処理中にエラーが発生しました: {ex.Message}");
                }

                var responseJson = JsonSerializer.Serialize(response);
                _logWriter.Write("INFO", "CommandLineIpc", $"Response={responseJson}");
                await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logWriter.Write(ex);
            }
        }
    }

    private async Task<CommandLineIpcResponse> HandleRequestAsync(string? requestJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            return new CommandLineIpcResponse(CommandLineExitCodes.InvalidArguments, "要求データが空です。");
        }

        CommandLineIpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CommandLineIpcRequest>(requestJson);
        }
        catch (JsonException)
        {
            return new CommandLineIpcResponse(CommandLineExitCodes.InvalidArguments, "要求データの形式が不正です。");
        }

        if (request?.Invocation is null)
        {
            return new CommandLineIpcResponse(CommandLineExitCodes.InvalidArguments, "要求内容を解釈できませんでした。");
        }

        return await _controlService.ExecuteAsync(request.Invocation, fromIpc: true, cancellationToken);
    }

    private static T EnsureNotNull<T>(T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var security = new PipeSecurity();
        var authenticatedUsers = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var accessRule = new PipeAccessRule(
            authenticatedUsers,
            PipeAccessRights.FullControl,
            AccessControlType.Allow);
        security.AddAccessRule(accessRule);
        return security;
    }
}
