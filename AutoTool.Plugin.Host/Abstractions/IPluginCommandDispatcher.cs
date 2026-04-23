using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Interface;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginCommandDispatcher
{
    ValueTask<bool> ExecuteAsync(
        PluginCommandListItem item,
        ICommandExecutionContext context,
        CancellationToken cancellationToken);
}

