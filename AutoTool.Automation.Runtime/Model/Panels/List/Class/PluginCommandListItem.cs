using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Automation.Runtime.Attributes;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Automation.Runtime.Lists;

/// <summary>
/// プラグイン由来コマンドの基本情報と追加設定 JSON を保持します。
/// </summary>
public partial class PluginCommandListItem : CommandListItem
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("プラグインID", EditorType.TextBox, Group = "プラグイン", Order = 1,
        Description = "このコマンドを提供するプラグインの識別子")]
    private string _pluginId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("設定JSON", EditorType.MultiLineTextBox, Group = "プラグイン", Order = 2,
        Description = "プラグインコマンド固有の追加設定を JSON で保持します")]
    private string _parameterJson = "{}";

    public PluginCommandListItem()
    {
        UpdateDescription();
    }

    public PluginCommandListItem(PluginCommandListItem? item = null) : base(item)
    {
        if (item is null)
        {
            UpdateDescription();
            return;
        }

        PluginId = item.PluginId;
        ParameterJson = item.ParameterJson;
        UpdateDescription();
    }

    public override ICommandListItem Clone()
    {
        return new PluginCommandListItem(this);
    }

    partial void OnPluginIdChanged(string value)
    {
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        Description = string.IsNullOrWhiteSpace(PluginId)
            ? "プラグインコマンド"
            : $"プラグイン:{PluginId}";
    }
}


