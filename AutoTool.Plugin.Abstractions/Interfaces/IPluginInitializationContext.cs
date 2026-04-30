using AutoTool.Plugin.Abstractions.Video;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginInitializationContext
{
    string HostVersion { get; }

    string PluginDirectoryPath { get; }

    IVideoStreamRegistry VideoStreams { get; }

    void Log(string message);
}
