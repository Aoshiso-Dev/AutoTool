using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class LoadedPluginCatalog(
    IPluginLoader pluginLoader,
    VideoStreamRegistry? videoStreamRegistry = null) : ILoadedPluginCatalog, IDisposable, IAsyncDisposable
{
    private readonly IPluginLoader _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
    private readonly VideoStreamRegistry? _videoStreamRegistry = videoStreamRegistry;
    private readonly object _syncRoot = new();
    private IReadOnlyList<LoadedPlugin>? _cachedPlugins;
    private bool _disposed;

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

    public void Dispose()
    {
        DisposeAsyncCore(CancellationToken.None).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        IReadOnlyList<LoadedPlugin> plugins;
        lock (_syncRoot)
        {
            plugins = _cachedPlugins ?? [];
            _disposed = true;
        }

        foreach (var plugin in plugins)
        {
            await UnregisterVideoStreamsAsync(plugin, cancellationToken).ConfigureAwait(false);
            await plugin.Instance.DisposeAsync(cancellationToken).ConfigureAwait(false);
            if (plugin.LoadContext.IsCollectible)
            {
                plugin.LoadContext.Unload();
            }
        }
    }

    private async ValueTask UnregisterVideoStreamsAsync(LoadedPlugin plugin, CancellationToken cancellationToken)
    {
        if (_videoStreamRegistry is null)
        {
            return;
        }

        var sourceIds = _videoStreamRegistry.GetSources()
            .Where(x => string.Equals(x.ProviderPluginId, plugin.Manifest.PluginId, StringComparison.Ordinal))
            .Select(static x => x.SourceId)
            .ToArray();

        foreach (var sourceId in sourceIds)
        {
            await _videoStreamRegistry.UnregisterAsync(sourceId, cancellationToken).ConfigureAwait(false);
        }
    }
}

