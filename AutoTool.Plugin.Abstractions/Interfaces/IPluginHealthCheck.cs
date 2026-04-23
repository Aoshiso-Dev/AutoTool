using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginHealthCheck
{
    ValueTask<PluginHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken);
}


