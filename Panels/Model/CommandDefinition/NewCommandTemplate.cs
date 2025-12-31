// 新しいコマンド追加テンプレート
// このファイルをコピーして新しいコマンドを簡単に追加できます

using CommunityToolkit.Mvvm.ComponentModel;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Commands;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.List.Class
{
    // ============================================
    // 新しいコマンドの追加手順:
    // 1. 下記のテンプレートをコピー
    // 2. クラス名を変更（例：MyNewCommandItem）
    // 3. CommandDefinition属性のパラメータを変更
    // 4. 必要なプロパティを追加
    // 5. Descriptionプロパティを実装
    // 
    // 自動で以下が生成されます：
    // - ItemType.GetTypes()に自動追加
    // - EditPanelViewModelの選択リストに自動追加
    // - ファクトリでの自動生成対応
    // - シリアライゼーション対応
    // ============================================

    /*
    /// <summary>
    /// 新しいコマンドのアイテム
    /// </summary>
    [SimpleCommandBinding(typeof(MyNewCommand), typeof(IMyNewCommandSettings))]
    [CommandDefinition("MyNewCommand", typeof(MyNewCommand), typeof(IMyNewCommandSettings), CommandCategory.Action)]
    public partial class MyNewCommandItem : CommandListItem, IMyNewCommandItem, IMyNewCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _myProperty = string.Empty;

        // Descriptionプロパティは必須
        public new string Description => $"MyNewCommand: {MyProperty}";

        public MyNewCommandItem() { }

        public MyNewCommandItem(MyNewCommandItem? item = null) : base(item)
        {
            if (item != null)
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
    // 対応するインターフェースもCommand.Interfaceに追加
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
    // 対応するCommand実装もCommand.Classに追加
    public class MyNewCommand : BaseCommand, ICommand, IMyNewCommand
    {
        new public IMyNewCommandSettings Settings => (IMyNewCommandSettings)base.Settings;

        public MyNewCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            // コマンドの実装
            OnDoingCommand?.Invoke(this, $"MyNewCommandを実行しました: {Settings.MyProperty}");
            return true;
        }
    }
    */
}