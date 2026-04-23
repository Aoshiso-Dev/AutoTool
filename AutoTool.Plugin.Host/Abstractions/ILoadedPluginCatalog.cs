using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface ILoadedPluginCatalog
{
    IReadOnlyList<LoadedPlugin> GetLoadedPlugins();
}

