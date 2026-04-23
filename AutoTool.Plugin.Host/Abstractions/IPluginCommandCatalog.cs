using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginCommandCatalog
{
    IReadOnlyList<PluginCommandDefinition> GetCommandDefinitions();
}

