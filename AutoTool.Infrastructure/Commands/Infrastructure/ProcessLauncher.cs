using System.Diagnostics;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// プロセス起動の実装
/// </summary>
public class ProcessLauncher : IProcessLauncher
{
    public Task StartAsync(string programPath, string? arguments = null, string? workingDirectory = null)
    {
        return StartAsync(programPath, arguments, workingDirectory, false, CancellationToken.None);
    }

    public async Task StartAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = programPath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = true,
            };

            var process = Process.Start(startInfo);
            
            if (waitForExit && process != null)
            {
                await process.WaitForExitAsync(cancellationToken);
            }
        }, cancellationToken);
    }
}

