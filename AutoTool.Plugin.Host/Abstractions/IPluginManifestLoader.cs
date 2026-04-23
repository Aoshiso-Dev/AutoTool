using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginManifestLoader
{
    PluginManifestLoadResult Load(string manifestPath);
}

