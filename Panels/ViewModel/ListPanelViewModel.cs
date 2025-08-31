using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.List.Class;
using MacroPanels.Message;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.List.Type;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.ViewModel
{
    public partial class ListPanelViewModel : ObservableObject
    {
        private object? _commandHistory;

        #region Properties
        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private CommandList _commandList = new();

        private int _selectedLineNumber = 0;
        public int SelectedLineNumber
        {
            get => _selectedLineNumber;
            set
            {
                SetProperty(ref _selectedLineNumber, value);
                OnSelectedLineNumberChanged();
            }
        }

        public ICommandListItem? SelectedItem
        {
            get
            {
                return CommandList.Items.FirstOrDefault(x => x.IsSelected == true);
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                var existingItem = CommandList.Items.FirstOrDefault(x => x.IsSelected == true);

                if (existingItem != null)
                {
                    var index = CommandList.Items.IndexOf(existingItem);

                    CommandList.Override(index, value);

                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                }
            }
        }

        private int _executedLineNumber = 0;
        public int ExecutedLineNumber
        {
            get => _executedLineNumber;
            set
            {
                SetProperty(ref _executedLineNumber, value);
                OnExecutedLineNumberChanged();
            }
        }
        #endregion

        public ListPanelViewModel()
        {
        }

        /// <summary>
        /// CommandHistoryManagerを設定
        /// </summary>
        public void SetCommandHistory(object commandHistory)
        {
            _commandHistory = commandHistory;
        }

        #region OnChanged
        private void OnSelectedLineNumberChanged()
        {
            CommandList.Items.ToList().ForEach(x => x.IsSelected = false);

            var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
            if (existingItem != null)
            {
                existingItem.IsSelected = true;
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(existingItem));
            }
        }

        private void OnExecutedLineNumberChanged()
        {
            CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
            var cmd = CommandList.Items.Where(x => x.LineNumber == ExecutedLineNumber).FirstOrDefault();
            if (cmd != null)
            {
                cmd.IsRunning = true;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }
        #endregion

        #region ListIntaraction
        public void Refresh()
        {
            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }

        public void Add(string itemType)
        {
            // CommandRegistry を使用して自動生成
            var item = CommandRegistry.CreateCommandItem(itemType);

            if (item != null)
            {
                item.ItemType = itemType;

                if(CommandList.Items.Count != 0 && SelectedLineNumber >= 0)
                {
                    CommandList.Insert(SelectedLineNumber + 1, item);
                }
                else
                {
                    CommandList.Add(item);
                }

                SelectedLineNumber = CommandList.Items.IndexOf(item);

                // 追加後にCollectionViewを更新
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                
                System.Diagnostics.Debug.WriteLine($"Added command: {item.ItemType} -> {CommandRegistry.DisplayOrder.GetDisplayName(item.ItemType)}");
            }
        }

        /// <summary>
        /// 指定位置にアイテムを挿入（Undo/Redo用）
        /// </summary>
        public void InsertAt(int index, ICommandListItem item)
        {
            if (index < 0) index = 0;
            if (index > CommandList.Items.Count) index = CommandList.Items.Count;

            CommandList.Insert(index, item);
            SelectedLineNumber = index;
            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }

        /// <summary>
        /// 指定位置のアイテムを削除（Undo/Redo用）
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < CommandList.Items.Count)
            {
                CommandList.RemoveAt(index);
                
                if (CommandList.Items.Count == 0)
                {
                    SelectedLineNumber = 0;
                }
                else if (index >= CommandList.Items.Count)
                {
                    SelectedLineNumber = CommandList.Items.Count - 1;
                }
                else
                {
                    SelectedLineNumber = index;
                }
                
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }

        /// <summary>
        /// 指定位置のアイテムを置換（Undo/Redo用）
        /// </summary>
        public void ReplaceAt(int index, ICommandListItem item)
        {
            if (index >= 0 && index < CommandList.Items.Count)
            {
                CommandList.Override(index, item);
                SelectedLineNumber = index;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }

        /// <summary>
        /// アイテムを移動（Undo/Redo用）
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < CommandList.Items.Count &&
                toIndex >= 0 && toIndex < CommandList.Items.Count &&
                fromIndex != toIndex)
            {
                CommandList.Move(fromIndex, toIndex);
                SelectedLineNumber = toIndex;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }

        /// <summary>
        /// アイテムを追加（Undo/Redo用）
        /// </summary>
        public void AddItem(ICommandListItem item)
        {
            CommandList.Add(item);
            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }

        public void Up()
        {
            if(SelectedLineNumber == 0)
            {
                return;
            }

            var selectedBak = SelectedLineNumber;
            CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
            SelectedLineNumber = selectedBak - 1;
        }

        public void Down()
        {
            if(SelectedLineNumber == CommandList.Items.Count - 1)
            {
                return;
            }

            var selectedBak = SelectedLineNumber;
            CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
            SelectedLineNumber = selectedBak + 1;
        }

        public void Delete()
        {
            if (SelectedItem == null)
            {
                return;
            }

            var index = CommandList.Items.IndexOf(SelectedItem);

            CommandList.Remove(SelectedItem);

            if (CommandList.Items.Count == 0)
            {
                SelectedLineNumber = 0;
            }
            else if (index == CommandList.Items.Count)
            {
                SelectedLineNumber = index - 1;
            }
            else
            {
                SelectedLineNumber = index;
            }
            
        }

        public void Clear()
        {
            CommandList.Clear();
            SelectedLineNumber = 0;
        }

        public void Save(string filePath = "")
        {
            CommandList.Save(filePath);
        }

        public void Load(string filePath = "")
        {
            CommandList.Load(filePath);
            SelectedLineNumber = 0;
            SelectedItem = CommandList.Items.FirstOrDefault();

            // 読み込み後にCommandRegistryを初期化して日本語表示名が正しく表示されるようにする
            CommandRegistry.Initialize();
            
            // CollectionViewを更新して日本語表示名を適用
            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            
            // 各アイテムのプロパティ変更通知を発火してUI更新
            foreach (var item in CommandList.Items)
            {
                if (item is System.ComponentModel.INotifyPropertyChanged notifyItem)
                {
                    // ItemTypeプロパティの変更を通知（コンバーターが再実行される）
                    var propertyInfo = item.GetType().GetProperty(nameof(item.ItemType));
                    if (propertyInfo != null)
                    {
                        // 現在の値を再設定してプロパティ変更通知を発火
                        var currentValue = item.ItemType;
                        item.ItemType = currentValue;
                    }
                }
            }
        }
        #endregion

        #region Call from MainWindowViewModel
        public int GetCount()
        {
            return CommandList.Items.Count;
        }

        public ICommandListItem? GetRunningItem()
        {
            return CommandList.Items.FirstOrDefault(x => x.IsRunning == true);
        }

        public ICommandListItem? GetItem(int lineNumber)
        {
            return CommandList.Items.FirstOrDefault(x => x.LineNumber == lineNumber);
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public void SetSelectedItem(ICommandListItem? item)
        {
            SelectedItem = item;
        }

        public void SetSelectedLineNumber(int lineNumber)
        {
            SelectedLineNumber = lineNumber;
        }

        public void Prepare()
        {
            CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
            CommandList.Items.ToList().ForEach(x => x.Progress = 0);

            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }
        #endregion
    }
}
