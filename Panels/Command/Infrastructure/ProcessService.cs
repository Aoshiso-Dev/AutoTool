using System.Diagnostics;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// プロセス実行サービスの実装
/// </summary>
public class ProcessService : IProcessService
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
