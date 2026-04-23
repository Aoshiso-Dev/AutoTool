using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class LoadedPluginCatalog(IPluginLoader pluginLoader) : ILoadedPluginCatalog
{
    private readonly IPluginLoader _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
    private readonly object _syncRoot = new();
    private IReadOnlyList<LoadedPlugin>? _cachedPlugins;

    public IReadOnlyList<LoadedPlugin> GetLoadedPlugins()
    {
        if (_cachedPlugins is not null)
        {
            return _cachedPlugins;
        }

        lock (_syncRoot)
        {
            if (_cachedPlugins is not null)
            {
                return _cachedPlugins;
            }

            _cachedPlugins = _pluginLoader.LoadAll()
                .Where(static x => x.IsLoaded && x.Plugin is not null)
                .Select(static x => x.Plugin!)
                .ToList();

            return _cachedPlugins;
        }
    }
}

