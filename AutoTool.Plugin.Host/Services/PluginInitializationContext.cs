using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.Video;

namespace AutoTool.Plugin.Host.Services;

internal sealed class PluginInitializationContext(
    string hostVersion,
    string pluginDirectoryPath,
    IVideoStreamRegistry videoStreams,
    Action<string>? logAction = null) : IPluginInitializationContext
{
    private readonly Action<string>? _logAction = logAction;

    public string HostVersion { get; } = hostVersion;

    public string PluginDirectoryPath { get; } = pluginDirectoryPath;

    public IVideoStreamRegistry VideoStreams { get; } = videoStreams ?? throw new ArgumentNullException(nameof(videoStreams));

    public void Log(string message)
    {
        _logAction?.Invoke(message);
    }
}
