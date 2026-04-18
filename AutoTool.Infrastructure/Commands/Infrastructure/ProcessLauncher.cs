using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// �v���Z�X�N���T�[�r�X�̎���
/// </summary>
public class ProcessLauncher(TimeProvider? timeProvider = null) : IProcessLauncher
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task StartAsync(string programPath, string? arguments = null, string? workingDirectory = null)
    {
        return StartAsync(programPath, arguments, workingDirectory, false, CancellationToken.None);
    }

    public async Task StartAsync(
        string programPath,
        string? arguments,
        string? workingDirectory,
        bool waitForExit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = programPath,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = true
        };

        using var process = Process.Start(startInfo);
        if (waitForExit && process is not null)
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<ProcessOutputLine> StartWithOutputAsync(
        string programPath,
        string? arguments = null,
        string? workingDirectory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = programPath,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var channel = Channel.CreateUnbounded<ProcessOutputLine>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            yield break;
        }

        Task stdoutTask = PumpReaderAsync(process.StandardOutput, isError: false, channel.Writer, cancellationToken);
        Task stderrTask = PumpReaderAsync(process.StandardError, isError: true, channel.Writer, cancellationToken);

        _ = Task.Run(async () =>
        {
            Exception? completionError = null;
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                completionError = ex;
            }
            finally
            {
                channel.Writer.TryComplete(completionError);
            }
        }, CancellationToken.None);

        await foreach (var line in channel.Reader.ReadAllAsync().ConfigureAwaitFalse(cancellationToken))
        {
            yield return line;
        }
    }

    private async Task PumpReaderAsync(
        StreamReader reader,
        bool isError,
        ChannelWriter<ProcessOutputLine> writer,
        CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            await writer.WriteAsync(
                new ProcessOutputLine(line, isError, _timeProvider.GetUtcNow()),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
