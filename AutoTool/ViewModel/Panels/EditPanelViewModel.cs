// EditPanelViewModel.cs
using AutoTool.Command.Base;
using AutoTool.Command.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// PropertyGrid の SelectedObject に渡す SelectedCommand を管理するVM。
    /// - コンボでコマンド種別選択 → 新規作成
    /// - いまの設定のまま複製
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly IServiceProvider _sp;

        // ======= UI バインディング先 =======
        // PropertyGrid が参照する選択中のコマンド（ClickCommand / ClickImageCommand / ClickImageAICommand）
        [ObservableProperty]
        private object? selectedCommand;

        // コンボに出すコマンド種別一覧
        public ObservableCollection<CommandKind> AvailableKinds { get; } = new();

        // 選択中のコマンド種別
        [ObservableProperty]
        private CommandKind? selectedKind;

        // 最近使った（任意で使ってください）
        public ObservableCollection<object> RecentCommands { get; } = new();

        // ======= CTOR =======
        public EditPanelViewModel(IServiceProvider sp)
        {
            _sp = sp;

            // ここに出したいコマンド種別を追加
            AvailableKinds.Add(new CommandKind("クリック", () => new ClickCommand(null, _sp)));
            AvailableKinds.Add(new CommandKind("画像クリック", () => new ClickImageCommand(null, _sp)));
            AvailableKinds.Add(new CommandKind("AI画像クリック", () => new ClickImageAICommand(null, _sp)));

            // 既定値：クリック
            SelectedKind = AvailableKinds[1];
            CreateNewCommand();
        }

        // ======= コマンド =======
        [RelayCommand]
        private void CreateNewCommand()
        {
            if (SelectedKind is null) return;
            var cmd = SelectedKind.Factory();
            SelectedCommand = cmd;
            if (!RecentCommands.Contains(cmd))
                RecentCommands.Insert(0, cmd);
        }

        [RelayCommand]
        private void DuplicateSelected()
        {
            if (SelectedCommand is null) return;

            // 同型の新インスタンスを作る（(parent, sp) のctor前提）
            var type = SelectedCommand.GetType();
            var newInstance = Activator.CreateInstance(type, new object?[] { null, _sp }) as IAutoToolCommand;
            if (newInstance is null) return;

            // 設定をざっくりコピー（RunCommand/CancelCommandなど実行系は除外）
            CopyPublicWritableProperties(SelectedCommand, newInstance);

            SelectedCommand = newInstance;
            RecentCommands.Insert(0, newInstance);
        }

        [RelayCommand]
        private void UseRecent(object? recent)
        {
            if (recent is null) return;
            SelectedCommand = recent;
        }

        // ======= ヘルパ =======
        private static void CopyPublicWritableProperties(object source, object target)
        {
            var srcProps = source.GetType()
                                 .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            var dstType = target.GetType();
            foreach (var sp in srcProps)
            {
                // 実行系や読み取り専用などはスキップ
                if (sp.Name is "RunCommand" or "CancelCommand")
                    continue;

                var dp = dstType.GetProperty(sp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (dp == null || !dp.CanWrite) continue;

                try
                {
                    var value = sp.GetValue(source);
                    dp.SetValue(target, value);
                }
                catch
                {
                    // 型不一致などは静かに無視
                }
            }
        }

        // ======= UI用：コンボ項目 =======
        public sealed class CommandKind
        {
            public string DisplayName { get; }
            public Func<IAutoToolCommand> Factory { get; }

            public CommandKind(string displayName, Func<IAutoToolCommand> factory)
            {
                DisplayName = displayName;
                Factory = factory;
            }

            public override string ToString() => DisplayName;
        }
    }
}
