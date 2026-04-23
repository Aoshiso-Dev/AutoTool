using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IAutoToolPlugin
{
    PluginDescriptor Descriptor { get; }

    ValueTask<PluginInitializationResult> InitializeAsync(
        IPluginInitializationContext context,
        CancellationToken cancellationToken);

    ValueTask DisposeAsync(CancellationToken cancellationToken);
}


