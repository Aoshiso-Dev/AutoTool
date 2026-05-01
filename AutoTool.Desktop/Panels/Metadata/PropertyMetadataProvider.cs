using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Model.Input;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Infrastructure.Panels;

/// <summary>
/// CommandListItem からプロパティのメタデータを取得するサービス
/// </summary>
public class PropertyMetadataProvider(IPluginCommandCatalog? pluginCommandCatalog = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly PropertyInfo DynamicValueProperty = typeof(PluginDynamicPropertyState).GetProperty(nameof(PluginDynamicPropertyState.Value))!;
    private readonly IPluginCommandCatalog? _pluginCommandCatalog = pluginCommandCatalog;

    /// <summary>
    /// アイテムからメタデータ付きプロパティを取得します
    /// </summary>
    public IEnumerable<PropertyMetadata> GetMetadata(ICommandListItem? item)
    {
        if (item is null)
        {
            yield break;
        }

        var pluginDefinition = ResolvePluginCommandDefinition(item);
        var reflectedProperties = item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<CommandPropertyAttribute>() is not null)
            .Where(p => pluginDefinition is null
                || pluginDefinition.Properties.Count == 0
                || !string.Equals(p.Name, nameof(PluginCommandListItem.ParameterJson), StringComparison.Ordinal))
            .Select(p => new PropertyMetadata
            {
                PropertyInfo = p,
                Attribute = p.GetCustomAttribute<CommandPropertyAttribute>()!,
                Target = item
            })
            .OrderBy(m => m.Group)
            .ThenBy(m => m.Order)
            .ToList();

        foreach (var metadata in reflectedProperties)
        {
            yield return metadata;
        }

        if (pluginDefinition is null || pluginDefinition.Properties.Count == 0 || item is not PluginCommandListItem pluginItem)
        {
            yield break;
        }

        foreach (var metadata in CreatePluginPropertyMetadata(pluginItem, pluginDefinition))
        {
            yield return metadata;
        }
    }

    /// <summary>
    /// アイテムからグループ化されたメタデータを取得します
    /// </summary>
    public IEnumerable<PropertyGroup> GetGroupedMetadata(ICommandListItem? item)
    {
        if (item is null)
        {
            yield break;
        }

        var groups = GetMetadata(item)
            .GroupBy(m => m.Group)
            .OrderBy(g => g.Min(m => m.Order))
            .Select(g => new PropertyGroup
            {
                GroupName = g.Key,
                Properties = [.. g.OrderBy(m => m.Order)]
            });

        foreach (var group in groups)
        {
            yield return group;
        }
    }

    /// <summary>
    /// アイテムが編集可能なプロパティを持つかどうかを返します
    /// </summary>
    public bool HasEditableProperties(ICommandListItem? item)
    {
        if (item is null)
        {
            return false;
        }

        return GetMetadata(item).Any();
    }

    private PluginCommandDefinition? ResolvePluginCommandDefinition(ICommandListItem item)
    {
        if (item is not PluginCommandListItem pluginItem || _pluginCommandCatalog is null)
        {
            return null;
        }

        return _pluginCommandCatalog.GetCommandDefinitions()
            .FirstOrDefault(definition =>
                string.Equals(definition.CommandType, pluginItem.ItemType, StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(pluginItem.PluginId)
                    || string.IsNullOrWhiteSpace(definition.PluginId)
                    || string.Equals(definition.PluginId, pluginItem.PluginId, StringComparison.Ordinal)));
    }

    private static IEnumerable<PropertyMetadata> CreatePluginPropertyMetadata(
        PluginCommandListItem item,
        PluginCommandDefinition definition)
    {
        var jsonObject = ParseParameterJson(item.ParameterJson);

        foreach (var property in definition.Properties
                     .OrderBy(p => p.Group ?? "プラグイン設定", StringComparer.Ordinal)
                     .ThenBy(p => p.Order)
                     .ThenBy(p => p.Name, StringComparer.Ordinal))
        {
            var state = new PluginDynamicPropertyState
            {
                Value = ReadJsonValue(jsonObject[property.Name], property),
            };

            yield return new PropertyMetadata
            {
                PropertyInfo = DynamicValueProperty,
                Attribute = new CommandPropertyAttribute(property.DisplayName, MapEditorType(property.EditorType))
                {
                    Group = property.Group ?? "プラグイン設定",
                    Order = property.Order,
                    Description = property.Description,
                    Options = property.Options.Count == 0 ? null : string.Join(',', property.Options),
                },
                Target = state,
                PropertyNameOverride = property.Name,
                DisplayNameOverride = property.DisplayName,
                DescriptionOverride = property.Description,
                GroupOverride = property.Group ?? "プラグイン設定",
                OrderOverride = property.Order,
                EditorTypeOverride = MapEditorType(property.EditorType),
                OptionsOverride = property.Options.Count == 0 ? null : [.. property.Options],
                FileFilterOverride = property.FileFilter,
                PropertyTypeOverride = state.Value?.GetType() ?? typeof(string),
                GetValueFunc = () => state.Value,
                SetValueAction = value =>
                {
                    state.Value = NormalizeValue(value, property);
                    jsonObject[property.Name] = ToJsonNode(state.Value);
                    item.ParameterJson = jsonObject.ToJsonString(JsonOptions);
                }
            };
        }
    }

    private static JsonObject ParseParameterJson(string parameterJson)
    {
        if (string.IsNullOrWhiteSpace(parameterJson))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(parameterJson) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static object? ReadJsonValue(JsonNode? node, PluginCommandPropertyDefinition property)
    {
        if (node is null)
        {
            return GetDefaultValue(property);
        }

        try
        {
            return MapEditorType(property.EditorType) switch
            {
                EditorType.CheckBox => node.GetValue<bool>(),
                EditorType.NumberBox => node.GetValue<int>(),
                EditorType.Slider => node.GetValue<double>(),
                EditorType.MouseButtonPicker => Enum.TryParse<CommandMouseButton>(node.GetValue<string>(), true, out var mouseButton)
                    ? mouseButton
                    : CommandMouseButton.Left,
                EditorType.KeyPicker => Enum.TryParse<CommandKey>(node.GetValue<string>(), true, out var key)
                    ? key
                    : CommandKey.None,
                _ => node.GetValue<string>()
            };
        }
        catch (InvalidOperationException)
        {
            return GetDefaultValue(property);
        }
        catch (FormatException)
        {
            return GetDefaultValue(property);
        }
    }

    private static object? GetDefaultValue(PluginCommandPropertyDefinition property)
    {
        return MapEditorType(property.EditorType) switch
        {
            EditorType.CheckBox => false,
            EditorType.NumberBox => 0,
            EditorType.Slider => 0d,
            EditorType.MouseButtonPicker => CommandMouseButton.Left,
            EditorType.KeyPicker => CommandKey.None,
            _ => string.Empty
        };
    }

    private static object? NormalizeValue(object? value, PluginCommandPropertyDefinition property)
    {
        return MapEditorType(property.EditorType) switch
        {
            EditorType.CheckBox => value is bool boolValue ? boolValue : false,
            EditorType.NumberBox => value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                double doubleValue => (int)Math.Round(doubleValue),
                _ => 0
            },
            EditorType.Slider => value switch
            {
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                int intValue => intValue,
                _ => 0d
            },
            EditorType.MouseButtonPicker => value is CommandMouseButton mouseButton ? mouseButton : CommandMouseButton.Left,
            EditorType.KeyPicker => value is CommandKey key ? key : CommandKey.None,
            _ => value?.ToString() ?? string.Empty
        };
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        return value switch
        {
            null => null,
            CommandMouseButton mouseButton => JsonValue.Create(mouseButton.ToString()),
            CommandKey key => JsonValue.Create(key.ToString()),
            bool boolValue => JsonValue.Create(boolValue),
            int intValue => JsonValue.Create(intValue),
            long longValue => JsonValue.Create(longValue),
            double doubleValue => JsonValue.Create(doubleValue),
            float floatValue => JsonValue.Create(floatValue),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static EditorType MapEditorType(string editorType)
    {
        return editorType.Trim() switch
        {
            var value when Enum.TryParse<EditorType>(value, true, out var parsed) => parsed,
            "MultiLine" => EditorType.MultiLineTextBox,
            "Textarea" => EditorType.MultiLineTextBox,
            _ => EditorType.TextBox
        };
    }

    private sealed class PluginDynamicPropertyState
    {
        public object? Value { get; set; }
    }
}

