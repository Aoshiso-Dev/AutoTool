using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using MacroPanels.Model.List.Interface;
using MacroPanels.List.Class;
using MacroPanels.Message;
using MacroPanels.Model.MacroFactory;
using MacroPanels.ViewModel.Shared;
using Microsoft.Extensions.Logging;
using System.Windows.Data;

namespace MacroPanels.ViewModel
{
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private CommandHistoryManager? _commandHistory;

        [ObservableProperty] 
        private CommandList _commandList = new();
        
        [ObservableProperty] 
        private ICommandListItem? _selectedItem;
        
        [ObservableProperty] 
        private int _selectedLineNumber = 0;
        
        [ObservableProperty] 
        private bool _isRunning = false;

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

        /// <summary>
        /// DI対応コンストラクタ
        /// </summary>
        public ListPanelViewModel(ILogger<ListPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("ListPanelViewModel をDI対応で初期化しています");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger.LogDebug("ListPanelViewModel の初期化を開始します");
                
                RegisterPropertyChangedEvents();
                
                _logger.LogDebug("ListPanelViewModel の初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel の初期化中にエラーが発生しました");
                throw;
            }
        }

        private void RegisterPropertyChangedEvents()
        {
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(SelectedLineNumber))
                {
                    OnSelectedLineNumberChanged();
                }
                else if (e.PropertyName == nameof(SelectedItem) && SelectedItem != null)
                {
                    _logger?.LogDebug("選択アイテム変更: {ItemType} (行 {LineNumber})", 
                        SelectedItem.ItemType, SelectedItem.LineNumber);
                    
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
                }
            };
        }

        private void OnSelectedLineNumberChanged()
        {
            try
            {
                CommandList.Items.ToList().ForEach(x => x.IsSelected = false);
                
                var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
                if (existingItem != null)
                {
                    existingItem.IsSelected = true;
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(existingItem));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "選択行番号変更中にエラーが発生しました");
            }
        }

        private void OnExecutedLineNumberChanged()
        {
            try
            {
                CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
                var cmd = CommandList.Items.Where(x => x.LineNumber == ExecutedLineNumber).FirstOrDefault();
                if (cmd != null)
                {
                    cmd.IsRunning = true;
                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "実行行番号変更中にエラーが発生しました");
            }
        }

        /// <summary>
        /// CommandHistoryManagerを設定
        /// </summary>
        public void SetCommandHistory(CommandHistoryManager? commandHistory)
        {
            _commandHistory = commandHistory;
            _logger?.LogDebug("CommandHistoryManager を設定しました");
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger?.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// UI表示を更新
        /// </summary>
        public void Refresh()
        {
            try
            {
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                _logger?.LogDebug("リストビューを更新しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "リスト更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// アイテムを追加
        /// </summary>
        public void Add(string itemType)
        {
            try
            {
                _logger?.LogDebug("アイテムを追加します: {ItemType}", itemType);
                
                var newItem = MacroPanels.Model.CommandDefinition.CommandRegistry.CreateCommandItem(itemType);
                if (newItem != null)
                {
                    newItem.ItemType = itemType;

                    if (CommandList.Items.Count != 0 && SelectedLineNumber >= 0)
                    {
                        CommandList.Insert(SelectedLineNumber + 1, newItem);
                    }
                    else
                    {
                        CommandList.Add(newItem);
                    }

                    SelectedLineNumber = CommandList.Items.IndexOf(newItem);
                    SelectedItem = newItem;

                    // UI更新
                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                    
                    _logger?.LogInformation("アイテムを追加しました: {ItemType} (合計 {Count}件)", 
                        itemType, CommandList.Items.Count);
                }
                else
                {
                    _logger?.LogWarning("アイテムの作成に失敗しました: {ItemType}", itemType);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム追加中にエラーが発生しました: {ItemType}", itemType);
                throw;
            }
        }

        /// <summary>
        /// 指定位置にアイテムを挿入（Undo/Redo用）
        /// </summary>
        public void InsertAt(int index, ICommandListItem item)
        {
            try
            {
                if (index < 0) index = 0;
                if (index > CommandList.Items.Count) index = CommandList.Items.Count;

                CommandList.Insert(index, item);
                SelectedLineNumber = index;
                SelectedItem = item;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                
                _logger?.LogDebug("アイテムを挿入しました: {ItemType} at {Index}", item.ItemType, index);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム挿入中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 指定位置のアイテムを削除（Undo/Redo用）
        /// </summary>
        public void RemoveAt(int index)
        {
            try
            {
                if (index >= 0 && index < CommandList.Items.Count)
                {
                    var item = CommandList.Items[index];
                    CommandList.RemoveAt(index);

                    if (CommandList.Items.Count == 0)
                    {
                        SelectedLineNumber = 0;
                        SelectedItem = null;
                    }
                    else if (index >= CommandList.Items.Count)
                    {
                        SelectedLineNumber = CommandList.Items.Count - 1;
                        SelectedItem = CommandList.Items.LastOrDefault();
                    }
                    else
                    {
                        SelectedLineNumber = index;
                        SelectedItem = CommandList.Items.ElementAtOrDefault(index);
                    }
                    
                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                    
                    // 削除後に選択状態の変更を通知
                    if (SelectedItem != null)
                    {
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
                    }
                    else
                    {
                        // アイテムがない場合はnullを送信
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                    }
                    
                    _logger?.LogDebug("アイテムを削除しました: {ItemType} at {Index}", item.ItemType, index);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム削除中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 指定位置のアイテムを置換（Undo/Redo用）
        /// </summary>
        public void ReplaceAt(int index, ICommandListItem item)
        {
            try
            {
                if (index >= 0 && index < CommandList.Items.Count)
                {
                    CommandList.Override(index, item);
                    SelectedLineNumber = index;
                    SelectedItem = item;
                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                    
                    _logger?.LogDebug("アイテムを置換しました: {ItemType} at {Index}", item.ItemType, index);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム置換中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// アイテムを移動（Undo/Redo用）
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            try
            {
                if (fromIndex >= 0 && fromIndex < CommandList.Items.Count &&
                    toIndex >= 0 && toIndex < CommandList.Items.Count &&
                    fromIndex != toIndex)
                {
                    CommandList.Move(fromIndex, toIndex);
                    SelectedLineNumber = toIndex;
                    CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                    
                    _logger?.LogDebug("アイテムを移動しました: {From} -> {To}", fromIndex, toIndex);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム移動中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// アイテムを追加（Undo/Redo用）
        /// </summary>
        public void AddItem(ICommandListItem item)
        {
            try
            {
                CommandList.Add(item);
                SelectedLineNumber = CommandList.Items.Count - 1;
                SelectedItem = item;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
                
                _logger?.LogDebug("アイテムを追加しました: {ItemType}", item.ItemType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム追加中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 選択アイテムを上に移動
        /// </summary>
        public void Up()
        {
            if (SelectedLineNumber == 0)
            {
                _logger?.LogDebug("最上位アイテムのため上移動できません");
                return;
            }

            try
            {
                var selectedBak = SelectedLineNumber;
                CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
                SelectedLineNumber = selectedBak - 1;
                
                _logger?.LogDebug("アイテムを上に移動しました: {FromIndex} -> {ToIndex}", 
                    selectedBak, SelectedLineNumber);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム上移動中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 選択アイテムを下に移動
        /// </summary>
        public void Down()
        {
            if (SelectedLineNumber == CommandList.Items.Count - 1)
            {
                _logger?.LogDebug("最下位アイテムのため下移動できません");
                return;
            }

            try
            {
                var selectedBak = SelectedLineNumber;
                CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
                SelectedLineNumber = selectedBak + 1;
                
                _logger?.LogDebug("アイテムを下に移動しました: {FromIndex} -> {ToIndex}", 
                    selectedBak, SelectedLineNumber);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム下移動中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 選択アイテムを削除
        /// </summary>
        public void Delete()
        {
            if (SelectedItem == null)
            {
                _logger?.LogDebug("削除対象のアイテムが選択されていません");
                return;
            }

            try
            {
                var index = CommandList.Items.IndexOf(SelectedItem);
                var itemType = SelectedItem.ItemType;
                
                CommandList.Remove(SelectedItem);

                if (CommandList.Items.Count == 0)
                {
                    SelectedLineNumber = 0;
                    SelectedItem = null;
                }
                else if (index == CommandList.Items.Count)
                {
                    SelectedLineNumber = index - 1;
                    SelectedItem = CommandList.Items.LastOrDefault();
                }
                else
                {
                    SelectedLineNumber = index;
                    SelectedItem = CommandList.Items.ElementAtOrDefault(index);
                }
                
                // 削除後に選択状態の変更を通知
                if (SelectedItem != null)
                {
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
                }
                else
                {
                    // アイテムがない場合はnullを送信
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                }
                
                _logger?.LogInformation("アイテムを削除しました: {ItemType} (残り {Count}件)", 
                    itemType, CommandList.Items.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテム削除中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 全アイテムをクリア
        /// </summary>
        public void Clear()
        {
            try
            {
                var count = CommandList.Items.Count;
                CommandList.Clear();
                SelectedLineNumber = 0;
                SelectedItem = null;
                
                // 全クリア後にEditPanelにnullを通知
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                
                _logger?.LogInformation("全アイテムをクリアしました: {Count}件削除", count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アイテムクリア中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// ファイルに保存
        /// </summary>
        public void Save(string filePath = "")
        {
            try
            {
                _logger?.LogInformation("ファイルに保存します: {FilePath}", 
                    string.IsNullOrEmpty(filePath) ? "(デフォルトパス)" : filePath);
                
                CommandList.Save(filePath);
                
                _logger?.LogInformation("ファイル保存が完了しました: {Count}件", CommandList.Items.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ファイル保存中にエラーが発生しました: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// ファイルから読み込み
        /// </summary>
        public void Load(string filePath = "")
        {
            try
            {
                _logger?.LogInformation("ファイルから読み込みます: {FilePath}", 
                    string.IsNullOrEmpty(filePath) ? "(デフォルトパス)" : filePath);
                
                CommandList.Load(filePath);
                SelectedLineNumber = 0;
                SelectedItem = CommandList.Items.FirstOrDefault();

                // 読み込み後にCommandRegistryを初期化して日本語表示名が正しく表示されるようにする
                MacroPanels.Model.CommandDefinition.CommandRegistry.Initialize();

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
                
                _logger?.LogInformation("ファイル読み込みが完了しました: {Count}件", CommandList.Items.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ファイル読み込み中にエラーが発生しました: {FilePath}", filePath);
                throw;
            }
        }

        // DI対応のメソッド（新規追加）
        public IEnumerable<ICommandListItem> GetItems()
        {
            return CommandList?.Items ?? Enumerable.Empty<ICommandListItem>();
        }

        public void SetItems(IEnumerable<ICommandListItem> items)
        {
            if (CommandList != null)
            {
                CommandList.Items.Clear();
                foreach (var item in items)
                {
                    CommandList.Items.Add(item);
                }
                _logger?.LogDebug("アイテムリストを設定しました: {Count}件", items.Count());
            }
        }

        public void RestoreItems(IEnumerable<ICommandListItem> items)
        {
            SetItems(items);
        }

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
            try
            {
                CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
                CommandList.Items.ToList().ForEach(x => x.Progress = 0);
                _logger?.LogDebug("ListPanelViewModel の準備を実行しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "準備処理中にエラーが発生しました");
            }
        }
    }
}
