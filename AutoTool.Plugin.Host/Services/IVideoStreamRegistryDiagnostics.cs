using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public interface IVideoStreamRegistryDiagnostics
{
    IReadOnlyList<VideoStreamRegistryIssue> GetIssues();

    int GetRegisteredSourceCount(string providerPluginId);
}
