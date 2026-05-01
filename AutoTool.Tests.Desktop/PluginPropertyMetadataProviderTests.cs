using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Infrastructure.Panels;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグイン定義の properties から編集メタデータを生成できることを確認します。
/// </summary>
public sealed class PluginPropertyMetadataProviderTests
{
    [Fact]
    public void GetMetadata_WithPluginProperties_ExpandsStructuredEditors()
    {
        var provider = new PropertyMetadataProvider(new FakePluginCommandCatalog(
        [
            new PluginCommandDefinition
            {
                PluginId = "Sample.Plugin",
                CommandType = "Sample.Plugin.ProviderCommand",
                DisplayName = "Provider Command",
                Category = "System",
                Properties =
                [
                    new PluginCommandPropertyDefinition
                    {
                        Name = "targetVariable",
                        DisplayName = "対象変数",
                        EditorType = "TextBox",
                        Group = "設定",
                        Order = 1,
                    },
                    new PluginCommandPropertyDefinition
                    {
                        Name = "enabled",
                        DisplayName = "有効",
                        EditorType = "CheckBox",
                        Group = "設定",
                        Order = 2,
                    },
                    new PluginCommandPropertyDefinition
                    {
                        Name = "settingsFile",
                        DisplayName = "設定ファイル",
                        EditorType = "FilePicker",
                        Group = "設定",
                        Order = 3,
                        FileFilter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    }
                ]
            }
        ]));

        var item = new PluginCommandListItem
        {
            ItemType = "Sample.Plugin.ProviderCommand",
            PluginId = "Sample.Plugin",
            ParameterJson = """{"targetVariable":"result","enabled":true}""",
        };

        var metadata = provider.GetMetadata(item).ToList();

        Assert.DoesNotContain(metadata, static x => x.PropertyName == nameof(PluginCommandListItem.ParameterJson));

        var variable = Assert.Single(metadata, static x => x.PropertyName == "targetVariable");
        Assert.Equal("対象変数", variable.DisplayName);
        Assert.Equal(EditorType.TextBox, variable.EditorType);
        Assert.Equal("result", variable.StringValue);

        var enabled = Assert.Single(metadata, static x => x.PropertyName == "enabled");
        Assert.Equal(EditorType.CheckBox, enabled.EditorType);
        Assert.True(enabled.BoolValue);

        var settingsFile = Assert.Single(metadata, static x => x.PropertyName == "settingsFile");
        Assert.Equal(EditorType.FilePicker, settingsFile.EditorType);
        Assert.Equal("JSON Files (*.json)|*.json|All Files (*.*)|*.*", settingsFile.FileFilter);

        variable.StringValue = "updated";
        enabled.BoolValue = false;

        Assert.Contains("updated", item.ParameterJson);
        Assert.Contains("\"enabled\":false", item.ParameterJson, StringComparison.Ordinal);
    }

    private sealed class FakePluginCommandCatalog(IReadOnlyList<PluginCommandDefinition> definitions) : IPluginCommandCatalog
    {
        private readonly IReadOnlyList<PluginCommandDefinition> _definitions = definitions;

        public IReadOnlyList<PluginCommandDefinition> GetCommandDefinitions() => _definitions;
    }
}

