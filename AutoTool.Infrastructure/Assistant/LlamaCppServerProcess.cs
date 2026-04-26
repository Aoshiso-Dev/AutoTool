using System.Diagnostics;
using AutoTool.Application.Assistant;

namespace AutoTool.Infrastructure.Assistant;

/// <summary>
/// llama.cpp サーバープロセスの起動状態を管理します。
/// </summary>
internal sealed class LlamaCppServerProcess : IDisposable
{
    private readonly Lock _gate = new();
    private Process? _process;

    public bool EnsureStarted(AssistantSettings settings, out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(settings);

        errorMessage = string.Empty;
        if (!settings.StartServerAutomatically)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(settings.LlamaServerPath))
        {
            errorMessage = "llama-server.exe のパスが設定されていません。";
            return false;
        }

        if (!File.Exists(settings.LlamaServerPath))
        {
            errorMessage = $"llama-server.exe が見つかりません: {settings.LlamaServerPath}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.ModelPath))
        {
            errorMessage = "GGUFモデルファイルのパスが設定されていません。";
            return false;
        }

        if (!File.Exists(settings.ModelPath))
        {
            errorMessage = $"GGUFモデルファイルが見つかりません: {settings.ModelPath}";
            return false;
        }

        lock (_gate)
        {
            if (_process is { HasExited: false })
            {
                return true;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = settings.LlamaServerPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(settings.LlamaServerPath) ?? Environment.CurrentDirectory
            };
            startInfo.ArgumentList.Add("-m");
            startInfo.ArgumentList.Add(settings.ModelPath);
            startInfo.ArgumentList.Add("--port");
            startInfo.ArgumentList.Add(settings.Port.ToString());
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(settings.ContextLength.ToString());

            try
            {
                _process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                errorMessage = $"llama.cpp の起動に失敗しました。{ex.Message}";
                return false;
            }

            if (_process is null)
            {
                errorMessage = "llama.cpp の起動に失敗しました。";
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            try
            {
                if (_process is { HasExited: false })
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
    }
}
