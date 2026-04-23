using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginCommandExecutor
{
    ValueTask<bool> ExecuteCommandAsync(
        PluginCommandExecutionRequest request,
        IPluginExecutionContext context,
        CancellationToken cancellationToken);
}

