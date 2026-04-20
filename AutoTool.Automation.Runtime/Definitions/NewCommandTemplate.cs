// 新規コマンド追加テンプレート
// このファイルをコピーして新しいコマンドを簡単に追加できます

using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

// ============================================
// 新規コマンドの追加手順:
// 1. 下記のテンプレートをコピー
// 2. クラス名を変更（例: MyNewCommandItem）
// 3. `CommandDefinition` 属性のパラメータを変更
// 4. 必要なプロパティを追加
// 5. `Description` プロパティを実装
// 
// これで以下が自動的に反映されます:
// - `ItemType.GetTypes()` に自動追加
// - `EditPanelViewModel` の選択リストに自動追加
// - ファクトリでの生成処理に対応
// - シリアライゼーションに対応
// ============================================

/*
/// <summary>
/// 新規コマンドのアイテム
/// </summary>
[SimpleCommandBinding(typeof(MyNewCommand), typeof(IMyNewCommandSettings))]
[CommandDefinition("MyNewCommand", typeof(MyNewCommand), typeof(IMyNewCommandSettings), CommandCategory.Action)]
public partial class MyNewCommandItem : CommandListItem, IMyNewCommandItem, IMyNewCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    private string _myProperty = string.Empty;

    // `Description` プロパティは必須
    public new string Description => $"MyNewCommand: {MyProperty}";

    public MyNewCommandItem() { }

    public MyNewCommandItem(MyNewCommandItem? item = null) : base(item)
    {
        if (item is not null)
        {
            MyProperty = item.MyProperty;
        }
    }

    public new ICommandListItem Clone()
    {
        return new MyNewCommandItem(this);
    }
}
*/

/*
// 対応するインターフェースを `Command.Interface` に追加
public interface IMyNewCommandItem : ICommandListItem
{
    string MyProperty { get; set; }
}

public interface IMyNewCommandSettings : ICommandSettings
{
    string MyProperty { get; set; }
}

public interface IMyNewCommand : ICommand
{
    new IMyNewCommandSettings Settings { get; }
}
*/

/*
// 対応する `Command` 実装を `Command.Class` に追加
public class MyNewCommand : BaseCommand, ICommand, IMyNewCommand
{
    new public IMyNewCommandSettings Settings => (IMyNewCommandSettings)base.Settings;

    public MyNewCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

    protected override async ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        // コマンドの実行
        OnDoingCommand?.Invoke(this, $"MyNewCommand を実行しました: {Settings.MyProperty}");
        return true;
    }
}
*/
