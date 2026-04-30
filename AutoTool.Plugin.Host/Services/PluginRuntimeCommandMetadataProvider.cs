using AutoTool.Commands.Commands;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginRuntimeCommandMetadataProvider(IPluginCommandCatalog commandCatalog) : IExternalCommandMetadataProvider
{
    private readonly IPluginCommandCatalog _commandCatalog = commandCatalog ?? throw new ArgumentNullException(nameof(commandCatalog));

    public IReadOnlyList<CommandMetadata> GetCommandMetadata()
    {
        return _commandCatalog.GetCommandDefinitions()
            .Select(Map)
            .ToList();
    }

    private static CommandMetadata Map(PluginCommandDefinition definition)
    {
        return new CommandMetadata
        {
            TypeName = definition.CommandType,
            ItemType = typeof(PluginCommandListItem),
            CommandType = typeof(PluginCommand),
            SettingsType = typeof(CommandSettings),
            Category = MapCategory(definition.Category),
            IsIfCommand = false,
            IsLoopCommand = false,
            IsEndCommand = false,
            DisplayPriority = 8,
            DisplaySubPriority = definition.Order,
            DisplayNameJa = definition.DisplayName,
            DisplayNameEn = definition.DisplayName,
            CustomCategoryNameJa = definition.Category,
            CustomCategoryNameEn = definition.Category,
            CanCreateCommand = true,
            ShowInCommandList = definition.ShowInCommandList,
            PluginId = definition.PluginId
        };
    }

    private static CommandCategory MapCategory(string category)
    {
        return category switch
        {
            "Click" => CommandCategory.Click,
            "Input" => CommandCategory.Input,
            "Wait" => CommandCategory.Wait,
            "Condition" => CommandCategory.Condition,
            "Control" => CommandCategory.Control,
            "AI" => CommandCategory.AI,
            "System" => CommandCategory.System,
            "Variable" => CommandCategory.Variable,
            _ => CommandCategory.Action,
        };
    }
}



