using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginQuickActionCatalog(
    ILoadedPluginCatalog loadedPluginCatalog,
    IPluginCommandCatalog pluginCommandCatalog) : IPluginQuickActionCatalog
{
    private readonly ILoadedPluginCatalog _loadedPluginCatalog = loadedPluginCatalog ?? throw new ArgumentNullException(nameof(loadedPluginCatalog));
    private readonly IPluginCommandCatalog _pluginCommandCatalog = pluginCommandCatalog ?? throw new ArgumentNullException(nameof(pluginCommandCatalog));
    private readonly object _syncRoot = new();
    private IReadOnlyList<PluginQuickActionDescriptor>? _cachedDescriptors;

    public IReadOnlyList<PluginQuickActionDescriptor> GetQuickActions()
    {
        if (_cachedDescriptors is not null)
        {
            return _cachedDescriptors;
        }

        lock (_syncRoot)
        {
            if (_cachedDescriptors is not null)
            {
                return _cachedDescriptors;
            }

            var commandDefinitions = _pluginCommandCatalog.GetCommandDefinitions();
            _cachedDescriptors = _loadedPluginCatalog.GetLoadedPlugins()
                .SelectMany(plugin => plugin.Manifest.QuickActions.Select(action => CreateDescriptor(plugin.Manifest, action, commandDefinitions)))
                .OrderBy(static x => x.Order)
                .ThenBy(static x => x.PluginId, StringComparer.Ordinal)
                .ThenBy(static x => x.ActionId, StringComparer.Ordinal)
                .ToList();

            return _cachedDescriptors;
        }
    }

    private static PluginQuickActionDescriptor CreateDescriptor(
        PluginManifest manifest,
        PluginQuickActionDefinition action,
        IReadOnlyList<PluginCommandDefinition> commandDefinitions)
    {
        var commandExists = commandDefinitions.Any(x =>
            string.Equals(x.PluginId, manifest.PluginId, StringComparison.Ordinal) &&
            string.Equals(x.CommandType, action.CommandType, StringComparison.Ordinal));

        return new PluginQuickActionDescriptor
        {
            PluginId = manifest.PluginId,
            ActionId = action.ActionId,
            DisplayName = action.DisplayName,
            ToolTip = action.ToolTip,
            Icon = action.Icon,
            Order = action.Order,
            CommandType = action.CommandType,
            ParameterJson = string.IsNullOrWhiteSpace(action.ParameterJson) ? "{}" : action.ParameterJson,
            IsAvailable = commandExists,
            UnavailableReason = commandExists
                ? null
                : $"対応するプラグインコマンドが見つかりません: {action.CommandType}",
        };
    }
}
