using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Interface;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginCommandDispatcher(ILoadedPluginCatalog loadedPluginCatalog) : IPluginCommandDispatcher
{
    private readonly ILoadedPluginCatalog _loadedPluginCatalog = loadedPluginCatalog ?? throw new ArgumentNullException(nameof(loadedPluginCatalog));

    public ValueTask<bool> ExecuteAsync(
        PluginCommandListItem item,
        ICommandExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(item.PluginId))
        {
            throw new InvalidOperationException("プラグインID が設定されていません。");
        }

        var plugin = _loadedPluginCatalog.GetLoadedPlugins()
            .FirstOrDefault(x => string.Equals(x.Manifest.PluginId, item.PluginId, StringComparison.Ordinal));
        if (plugin is null)
        {
            throw new InvalidOperationException($"プラグインが読み込まれていません: {item.PluginId}");
        }

        if (plugin.CommandExecutor is null)
        {
            throw new InvalidOperationException($"プラグインはコマンド実行を提供していません: {item.PluginId}");
        }

        var request = new PluginCommandExecutionRequest
        {
            PluginId = item.PluginId,
            CommandType = item.ItemType,
            ParameterJson = string.IsNullOrWhiteSpace(item.ParameterJson) ? "{}" : item.ParameterJson,
        };

        return plugin.CommandExecutor.ExecuteCommandAsync(
            request,
            new PluginExecutionContextAdapter(context),
            cancellationToken);
    }
}

