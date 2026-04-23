using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginLoader
{
    PluginLoadResult Load(PluginManifestLoadResult manifestLoadResult);

    IReadOnlyList<PluginLoadResult> LoadAll();
}
