using System.Collections.ObjectModel;
using System.IO;
using AutoTool.Application.Ports;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Serialization;

public class CommandListFileGateway(
    IMacroFileSerializer macroFileSerializer,
    ICommandDefinitionProvider commandDefinitionProvider) : ICommandListFileGateway
{
    private readonly IMacroFileSerializer _macroFileSerializer = macroFileSerializer;
    private readonly ICommandDefinitionProvider _commandDefinitionProvider = commandDefinitionProvider;

    public void Save(IReadOnlyList<ICommandListItem> items, string filePath)
    {
        ArgumentNullException.ThrowIfNull(items);
        _macroFileSerializer.SerializeToFile(items, filePath);
    }

    public IReadOnlyList<ICommandListItem> Load(string filePath)
    {
        var deserializedItems = _macroFileSerializer.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
        if (deserializedItems is null)
        {
            return [];
        }

        List<ICommandListItem> loadedItems = [];
        foreach (var item in deserializedItems)
        {
            if (string.IsNullOrWhiteSpace(item.ItemType) || string.Equals(item.ItemType, "None", StringComparison.Ordinal))
            {
                if (CommandMetadataCatalog.TryGetByItemType(item.GetType(), out var metadata))
                {
                    item.ItemType = metadata.TypeName;
                }
            }

            var itemType = _commandDefinitionProvider.GetItemType(item.ItemType);
            if (itemType is null)
            {
                throw new InvalidDataException($"不明な ItemType: {item.ItemType}");
            }

            try
            {
                var newItem = (ICommandListItem)Activator.CreateInstance(itemType, item)!;
                loadedItems.Add(newItem);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"型 {item.ItemType} のアイテム作成に失敗しました: {ex.Message}");
            }
        }

        return loadedItems;
    }
}
