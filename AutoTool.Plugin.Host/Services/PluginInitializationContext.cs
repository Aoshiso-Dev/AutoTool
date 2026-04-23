using AutoTool.Plugin.Abstractions.Interfaces;

namespace AutoTool.Plugin.Host.Services;

internal sealed class PluginInitializationContext(
    string hostVersion,
    string pluginDirectoryPath,
    Action<string>? logAction = null) : IPluginInitializationContext
{
    private readonly Action<string>? _logAction = logAction;

    public string HostVersion { get; } = hostVersion;

    public string PluginDirectoryPath { get; } = pluginDirectoryPath;

    public void Log(string message)
    {
        _logAction?.Invoke(message);
    }
}
