using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Shared.UndoRedoCommands;
using AutoTool.Message;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：ListPanelViewModel（実際のコマンド操作機能付き）
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly CommandHistoryManager _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private CommandList _commandList = new();

        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedLineNumber = 0;

        /// <summary>
        /// Phase 5完全統合版コンストラクタ
        /// </summary>
        public ListPanelViewModel(ILogger<ListPanelViewModel> logger, CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));

            SetupMessaging();
            InitializeCommands();

            _logger.LogInformation("Phase 5完全統合ListPanelViewModel初期化完了");
        }

        private void SetupMessaging()
        {
            // メッセージを受信してコマンド操作を実行
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) =>
            {
                AddCommand(m.ItemType);
            });

            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) =>
            {
                DeleteSelectedCommand();
            });

            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) =>
            {
                MoveCommandUp();
            });

            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) =>
            {
                MoveCommandDown();
            });

            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) =>
            {
                ClearAllCommands();
            });

            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (r, m) =>
            {
                if (string.IsNullOrEmpty(m.FilePath))
                {
                    Save(); // デフォルト保存
                }
                else
                {
                    Save(m.FilePath); // 指定パス保存
                }
            });

            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (r, m) =>
            {
                if (string.IsNullOrEmpty(m.FilePath))
                {
                    Load(); // デフォルト読み込み
                }
                else
                {
                    Load(m.FilePath); // 指定パス読み込み
                }
            });
        }

        private void InitializeCommands()
        {
            // 初期サンプルコマンドを追加
            var sampleCommands = new[]
            {
                CreateBasicCommand("Wait", "サンプル待機コマンド"),
                CreateBasicCommand("Click", "サンプルクリックコマンド"),
                CreateBasicCommand("Loop", "サンプルループコマンド")
            };

            foreach (var command in sampleCommands)
            {
                Items.Add(command);
                _commandList.Items.Add(command);
            }

            UpdateLineNumbers();
            _logger.LogInformation("初期サンプルコマンドを追加しました: {Count}個", sampleCommands.Length);
        }

        /// <summary>
        /// コマンドを追加（Undo/Redo対応）
        /// </summary>
        public void AddCommand(string itemType)
        {
            try
            {
                _logger.LogDebug("コマンド追加開始: {ItemType}", itemType);

                // 新しいコマンドを作成
                var newItem = CreateBasicCommand(itemType, $"{itemType}コマンド");
                var targetIndex = Math.Max(0, _selectedLineNumber + 1);

                var addCommand = new AddItemCommand(
                    newItem,
                    targetIndex,
                    (item, index) =>
                    {
                        InsertAt(index, item);
                        _logger.LogDebug("コマンド追加実行: {ItemType} at {Index}", item.ItemType, index);
                    },
                    (index) =>
                    {
                        RemoveAt(index);
                        _logger.LogDebug("コマンド追加取り消し: at {Index}", index);
                    }
                );

                _commandHistory.ExecuteCommand(addCommand);
                _logger.LogInformation("コマンド追加完了: {ItemType} at {Index}", itemType, targetIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド追加中にエラーが発生しました: {ItemType}", itemType);
            }
        }

        /// <summary>
        /// 選択されたコマンドを削除（Undo/Redo対応）
        /// </summary>
        public void DeleteSelectedCommand()
        {
            try
            {
                if (SelectedItem == null)
                {
                    _logger.LogDebug("削除対象のアイテムが選択されていません");
                    return;
                }

                var selectedIndex = _selectedLineNumber;
                var itemToDelete = SelectedItem.Clone();

                var removeCommand = new RemoveItemCommand(
                    itemToDelete,
                    selectedIndex,
                    (item, index) =>
                    {
                        InsertAt(index, item);
                        _logger.LogDebug("コマンド削除取り消し: {ItemType} at {Index}", item.ItemType, index);
                    },
                    (index) =>
                    {
                        RemoveAt(index);
                        _logger.LogDebug("コマンド削除実行: at {Index}", index);
                    }
                );

                _commandHistory.ExecuteCommand(removeCommand);
                _logger.LogInformation("コマンド削除完了: index {Index}", selectedIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド削除中にエラーが発生しました");
            }
        }

        /// <summary>
        /// コマンドを上に移動（Undo/Redo対応）
        /// </summary>
        public void MoveCommandUp()
        {
            try
            {
                var fromIndex = _selectedLineNumber;
                var toIndex = fromIndex - 1;

                if (toIndex < 0)
                {
                    _logger.LogDebug("最上位アイテムのため上移動できません");
                    return;
                }

                var moveCommand = new MoveItemCommand(
                    fromIndex, toIndex,
                    (from, to) =>
                    {
                        MoveItem(from, to);
                        _selectedLineNumber = to;
                        _logger.LogDebug("コマンド上移動: {From} → {To}", from, to);
                    }
                );

                _commandHistory.ExecuteCommand(moveCommand);
                _logger.LogInformation("コマンド上移動完了: {From} → {To}", fromIndex, toIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド上移動中にエラーが発生しました");
            }
        }

        /// <summary>
        /// コマンドを下に移動（Undo/Redo対応）
        /// </summary>
        public void MoveCommandDown()
        {
            try
            {
                var fromIndex = _selectedLineNumber;
                var toIndex = fromIndex + 1;

                if (toIndex >= Items.Count)
                {
                    _logger.LogDebug("最下位アイテムのため下移動できません");
                    return;
                }

                var moveCommand = new MoveItemCommand(
                    fromIndex, toIndex,
                    (from, to) =>
                    {
                        MoveItem(from, to);
                        _selectedLineNumber = to;
                        _logger.LogDebug("コマンド下移動: {From} → {To}", from, to);
                    }
                );

                _commandHistory.ExecuteCommand(moveCommand);
                _logger.LogInformation("コマンド下移動完了: {From} → {To}", fromIndex, toIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド下移動中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 全コマンドをクリア（Undo/Redo対応）
        /// </summary>
        public void ClearAllCommands()
        {
            try
            {
                var currentItems = Items.ToList();
                if (!currentItems.Any())
                {
                    _logger.LogDebug("クリア対象のアイテムがありません");
                    return;
                }

                var clearCommand = new ClearAllCommand(
                    currentItems,
                    () =>
                    {
                        Clear();
                        _logger.LogDebug("全コマンドクリア実行");
                    },
                    (items) =>
                    {
                        RestoreItems(items);
                        _logger.LogDebug("全コマンドクリア取り消し: {Count}件復元", items.Count());
                    }
                );

                _commandHistory.ExecuteCommand(clearCommand);
                _logger.LogInformation("全コマンドクリア完了: {Count}件削除", currentItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全コマンドクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// ファイルに保存
        /// </summary>
        public void Save(string? filePath = null)
        {
            try
            {
                var path = filePath ?? Path.Combine(Environment.CurrentDirectory, "default_macro.json");
                var data = new
                {
                    Commands = Items.Select(item => new
                    {
                        ItemType = item.ItemType,
                        Comment = item.Comment,
                        IsEnable = item.IsEnable,
                        LineNumber = item.LineNumber,
                        NestLevel = item.NestLevel
                    }).ToArray()
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                File.WriteAllText(path, jsonContent);
                _logger.LogInformation("マクロファイル保存完了: {Path}, {Count}個のコマンド", path, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイル保存中にエラーが発生しました: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// ファイルから読み込み
        /// </summary>
        public void Load(string? filePath = null)
        {
            try
            {
                var path = filePath ?? Path.Combine(Environment.CurrentDirectory, "default_macro.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("読み込み対象ファイルが存在しません: {Path}", path);
                    return;
                }

                var jsonContent = File.ReadAllText(path);
                using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
                
                Clear();

                if (document.RootElement.TryGetProperty("Commands", out var commandsElement))
                {
                    foreach (var commandElement in commandsElement.EnumerateArray())
                    {
                        var itemType = commandElement.GetProperty("ItemType").GetString() ?? "Unknown";
                        var comment = commandElement.GetProperty("Comment").GetString() ?? "";
                        var isEnable = commandElement.GetProperty("IsEnable").GetBoolean();

                        var item = CreateBasicCommand(itemType, comment);
                        item.IsEnable = isEnable;
                        
                        Items.Add(item);
                        _commandList.Items.Add(item);
                    }
                }

                UpdateLineNumbers();
                _commandHistory.Clear(); // ファイル読み込み後は履歴をクリア
                _logger.LogInformation("マクロファイル読み込み完了: {Path}, {Count}個のコマンド", path, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイル読み込み中にエラーが発生しました: {FilePath}", filePath);
                throw;
            }
        }

        #region 内部操作メソッド

        public void InsertAt(int index, ICommandListItem item)
        {
            Items.Insert(index, item);
            _commandList.Items.Insert(index, item);
            UpdateLineNumbers();
            OnPropertyChanged(nameof(Items));
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);
                _commandList.Items.RemoveAt(index);
                UpdateLineNumbers();
                
                // 選択位置を調整
                if (_selectedLineNumber >= Items.Count)
                {
                    _selectedLineNumber = Math.Max(0, Items.Count - 1);
                }
                OnPropertyChanged(nameof(Items));
            }
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < Items.Count && toIndex >= 0 && toIndex < Items.Count)
            {
                var item = Items[fromIndex];
                Items.RemoveAt(fromIndex);
                Items.Insert(toIndex, item);

                var commandItem = _commandList.Items[fromIndex];
                _commandList.Items.RemoveAt(fromIndex);
                _commandList.Items.Insert(toIndex, commandItem);

                UpdateLineNumbers();
                OnPropertyChanged(nameof(Items));
            }
        }

        public void ReplaceAt(int index, ICommandListItem newItem)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items[index] = newItem;
                _commandList.Items[index] = newItem;
                UpdateLineNumbers();
                OnPropertyChanged(nameof(Items));
            }
        }

        public void Clear()
        {
            Items.Clear();
            _commandList.Items.Clear();
            _selectedLineNumber = 0;
            SelectedItem = null;
            OnPropertyChanged(nameof(Items));
        }

        private void RestoreItems(IEnumerable<ICommandListItem> items)
        {
            Clear();
            foreach (var item in items)
            {
                Items.Add(item.Clone());
                _commandList.Items.Add(item.Clone());
            }
            UpdateLineNumbers();
            OnPropertyChanged(nameof(Items));
        }

        private BasicCommandItem CreateBasicCommand(string itemType, string comment)
        {
            return new BasicCommandItem
            {
                ItemType = itemType,
                Comment = comment,
                IsEnable = true,
                LineNumber = Items.Count + 1,
                NestLevel = 0
            };
        }

        private void UpdateLineNumbers()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        #endregion

        #region パブリックメソッド

        public int GetCount() => Items.Count;

        public ICommandListItem? GetItem(int lineNumber)
        {
            var index = lineNumber - 1;
            return index >= 0 && index < Items.Count ? Items[index] : null;
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(SelectedItem));
        }

        public void Prepare()
        {
            _logger.LogDebug("Phase 5完全統合ListPanelViewModel準備完了");
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        #endregion
    }
}