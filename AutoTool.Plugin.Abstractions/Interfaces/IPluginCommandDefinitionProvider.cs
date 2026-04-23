using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginCommandDefinitionProvider
{
    IReadOnlyList<PluginCommandDefinition> GetCommandDefinitions();
}


