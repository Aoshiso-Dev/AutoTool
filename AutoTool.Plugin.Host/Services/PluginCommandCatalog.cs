using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginCommandCatalog(ILoadedPluginCatalog loadedPluginCatalog) : IPluginCommandCatalog
{
    private readonly ILoadedPluginCatalog _loadedPluginCatalog = loadedPluginCatalog ?? throw new ArgumentNullException(nameof(loadedPluginCatalog));
    private readonly object _syncRoot = new();
    private IReadOnlyList<PluginCommandDefinition>? _cachedDefinitions;

    public IReadOnlyList<PluginCommandDefinition> GetCommandDefinitions()
    {
        if (_cachedDefinitions is not null)
        {
            return _cachedDefinitions;
        }

        lock (_syncRoot)
        {
            if (_cachedDefinitions is not null)
            {
                return _cachedDefinitions;
            }

            Dictionary<string, PluginCommandDefinition> map = new(StringComparer.Ordinal);

            foreach (var plugin in _loadedPluginCatalog.GetLoadedPlugins())
            {
                var manifest = plugin.Manifest;

                foreach (var command in manifest.Commands)
                {
                    var normalized = NormalizeCommand(command, manifest);
                    map[normalized.CommandType] = normalized;
                }

                if (plugin.CommandDefinitionProvider is null)
                {
                    continue;
                }

                foreach (var command in plugin.CommandDefinitionProvider.GetCommandDefinitions())
                {
                    var normalized = NormalizeCommand(command, manifest);
                    map[normalized.CommandType] = normalized;
                }
            }

            _cachedDefinitions = map.Values.ToList();
            return _cachedDefinitions;
        }
    }

    private static PluginCommandDefinition NormalizeCommand(PluginCommandDefinition command, PluginManifest manifest)
    {
        return command with
        {
            PluginId = string.IsNullOrWhiteSpace(command.PluginId)
                ? manifest.PluginId
                : command.PluginId,
            RequiredPermissions = command.RequiredPermissions.Count == 0
                ? manifest.Permissions
                : command.RequiredPermissions,
        };
    }
}


