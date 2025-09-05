using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// ListPanelViewModel (Phase 5統合版) - CommandRegistry -> DirectCommandRegistry対応
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject, IRecipient<RunMessage>, IRecipient<StopMessage>, IRecipient<AddMessage>, IRecipient<ClearMessage>, IRecipient<UpMessage>, IRecipient<DownMessage>, IRecipient<DeleteMessage>, IRecipient<UndoMessage>, IRecipient<RedoMessage>, IRecipient<LoadMessage>, IRecipient<SaveMessage>
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICommandListItemFactory _commandListItemFactory;
        private readonly IRecentFileService _recentFileService;

        private ICommand? _currentExecutingCommand;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private ICommandListItem? _currentExecutingItem;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _canExecute = true;

        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private int _executedItems;

        [ObservableProperty]
        private int _currentLineNumber;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private ObservableCollection<string> _executionLog = new();

        [ObservableProperty]
        private bool _isLogVisible = false;

        [ObservableProperty]
        private string _currentFileName = "新規ファイル";

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private ObservableCollection<string> _undoStack = new();

        [ObservableProperty]
        private ObservableCollection<string> _redoStack = new();

        [ObservableProperty]
        private TimeSpan _elapsedTime;

        [ObservableProperty]
        private string _currentCommandDescription = string.Empty;

        /// <summary>
        /// アイテムが存在するかどうか
        /// </summary>
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// Undoが可能かどうか
        /// </summary>
        public bool CanUndo => UndoStack.Count > 0;

        /// <summary>
        /// Redoが可能かどうか
        /// </summary>
        public bool CanRedo => RedoStack.Count > 0;

        public ListPanelViewModel(
            ILogger<ListPanelViewModel> logger,
            IServiceProvider serviceProvider,
            ICommandListItemFactory commandListItemFactory,
            IRecentFileService recentFileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commandListItemFactory = commandListItemFactory ?? throw new ArgumentNullException(nameof(commandListItemFactory));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));

            TotalItems = Items.Count;

            // メッセージングシステムに登録
            WeakReferenceMessenger.Default.Register<RunMessage>(this);
            WeakReferenceMessenger.Default.Register<StopMessage>(this);
            WeakReferenceMessenger.Default.Register<AddMessage>(this);
            WeakReferenceMessenger.Default.Register<ClearMessage>(this);
            WeakReferenceMessenger.Default.Register<UpMessage>(this);
            WeakReferenceMessenger.Default.Register<DownMessage>(this);
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this);
            WeakReferenceMessenger.Default.Register<UndoMessage>(this);
            WeakReferenceMessenger.Default.Register<RedoMessage>(this);
            WeakReferenceMessenger.Default.Register<LoadMessage>(this);
            WeakReferenceMessenger.Default.Register<SaveMessage>(this);

            _logger.LogInformation("ListPanelViewModel (Phase 5) が初期化されました");

            // プロパティ変更の監視
            Items.CollectionChanged += (s, e) =>
            {
                TotalItems = Items.Count;
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(HasItems));
            };
        }

        public void Receive(RunMessage message)
        {
            _logger.LogInformation("マクロ実行開始");
            _ = Task.Run(async () => await ExecuteMacroAsync());
        }

        public void Receive(StopMessage message)
        {
            _logger.LogInformation("マクロ実行停止要求");
            StopMacro();
        }

        public void Receive(AddMessage message)
        {
            _logger.LogDebug("コマンド追加要求: {ItemType}", message.ItemType);
            AddItem(message.ItemType);
        }

        public void Receive(ClearMessage message)
        {
            _logger.LogDebug("リストクリア要求");
            ClearAll();
        }

        public void Receive(UpMessage message)
        {
            _logger.LogDebug("アイテム上移動要求");
            MoveUp();
        }

        public void Receive(DownMessage message)
        {
            _logger.LogDebug("アイテム下移動要求");
            MoveDown();
        }

        public void Receive(DeleteMessage message)
        {
            _logger.LogDebug("アイテム削除要求");
            DeleteSelected();
        }

        public void Receive(UndoMessage message)
        {
            _logger.LogDebug("Undo要求");
            Undo();
        }

        public void Receive(RedoMessage message)
        {
            _logger.LogDebug("Redo要求");
            Redo();
        }

        public void Receive(LoadMessage message)
        {
            _logger.LogDebug("ファイル読み込み要求");
            _ = LoadFileAsync();
        }

        public void Receive(SaveMessage message)
        {
            _logger.LogDebug("ファイル保存要求");
            _ = SaveFileAsync();
        }

        // 実行中コマンドの表示名を更新する処理
        partial void OnCurrentExecutingItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                var displayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
                CurrentCommandDescription = $"実行中: {displayName} (行 {value.LineNumber})";
            }
            else
            {
                CurrentCommandDescription = string.Empty;
            }
        }

        // 選択アイテム変更時の処理を追加
        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            try
            {
                _logger.LogDebug("=== ListPanel選択変更: {OldItem} -> {NewItem} ===", 
                    _selectedItem?.ItemType ?? "null", value?.ItemType ?? "null");

                if (value != null)
                {
                    _logger.LogInformation("?? コマンド選択: {ItemType} (行番号: {LineNumber}, タイプ: {ActualType})", 
                        value.ItemType, value.LineNumber, value.GetType().Name);

                    // EditPanelに選択変更を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                    
                    // 選択アイテムを強調表示するためIsSelectedを更新
                    foreach (var item in Items)
                    {
                        item.IsSelected = (item == value);
                    }

                    StatusMessage = $"選択: {DirectCommandRegistry.DisplayOrder.GetDisplayName(value.ItemType)} (行 {value.LineNumber})";
                }
                else
                {
                    _logger.LogDebug("コマンド選択解除");
                    
                    // EditPanelに選択解除を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                    
                    // 全ての選択状態をクリア
                    foreach (var item in Items)
                    {
                        item.IsSelected = false;
                    }

                    StatusMessage = "コマンドが選択されていません";
                }

                _logger.LogDebug("=== ListPanel選択変更完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectedItem変更処理中にエラー");
            }
        }

        private void AddItem(string itemType)
        {
            try
            {
                _logger.LogInformation("=== コマンド追加処理開始: {ItemType} ===", itemType);

                // 1. UniversalCommandItemとして追加を試行（DirectCommandRegistry使用）
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        universalItem.LineNumber = GetNextLineNumber();
                        universalItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";
                        
                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, universalItem);
                        
                        SelectedItem = universalItem;
                        SelectedIndex = Items.IndexOf(universalItem);
                        
                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(universalItem));
                        
                        _logger.LogInformation("? UniversalCommandItemとして追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})", 
                            itemType, universalItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "UniversalCommandItem作成失敗、従来のFactoryを使用: {ItemType}", itemType);
                }

                // 2. 従来のCommandListItemFactoryでフォールバック
                var newItem = _commandListItemFactory.CreateItem(itemType);
                if (newItem != null)
                {
                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明";
                    
                    var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                    Items.Insert(insertIndex, newItem);
                    
                    SelectedItem = newItem;
                    SelectedIndex = Items.IndexOf(newItem);
                    
                    // EditPanelに選択変更を通知
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                    
                    _logger.LogInformation("? CommandListItemFactoryで追加成功: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})", 
                        itemType, newItem.LineNumber, insertIndex);
                    StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました";
                }
                else
                {
                    // 3. 最終フォールバック: BasicCommandItem
                    try
                    {
                        var fallbackItem = new AutoTool.Model.List.Type.BasicCommandItem
                        {
                            ItemType = itemType,
                            LineNumber = GetNextLineNumber(),
                            IsEnable = true,
                            Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}の説明"
                        };
                        
                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, fallbackItem);
                        
                        SelectedItem = fallbackItem;
                        SelectedIndex = Items.IndexOf(fallbackItem);
                        
                        // EditPanelに選択変更を通知
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(fallbackItem));
                        
                        _logger.LogWarning("?? BasicCommandItemとしてフォールバック追加: {ItemType} (行番号: {LineNumber}, 挿入位置: {Index})", 
                            itemType, fallbackItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}を追加しました（基本モード）";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "? BasicCommandItem作成も失敗: {ItemType}", itemType);
                        StatusMessage = $"コマンド追加に失敗しました: {itemType}";
                        return;
                    }
                }

                _logger.LogInformation("=== コマンド追加処理完了: {ItemType} (総アイテム数: {TotalCount}) ===", 
                    itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? AddItem中にエラー: {ItemType}", itemType);
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        private void ClearAll()
        {
            try
            {
                Items.Clear();
                SelectedItem = null;
                SelectedIndex = -1;
                StatusMessage = "すべてのアイテムをクリアしました";
                _logger.LogDebug("全アイテムクリア完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリア中にエラー");
                StatusMessage = $"クリアエラー: {ex.Message}";
            }
        }

        private void MoveUp()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex > 0)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index - 1, SelectedItem);
                    SelectedIndex = index - 1;
                    StatusMessage = "アイテムを上に移動しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上移動中にエラー");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        private void MoveDown()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex < Items.Count - 1)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index + 1, SelectedItem);
                    SelectedIndex = index + 1;
                    StatusMessage = "アイテムを下に移動しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下移動中にエラー");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        private void DeleteSelected()
        {
            try
            {
                if (SelectedItem != null)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.Remove(SelectedItem);
                    
                    // 選択位置を調整
                    if (Items.Count > 0)
                    {
                        if (index >= Items.Count) index = Items.Count - 1;
                        SelectedItem = Items[index];
                        SelectedIndex = index;
                    }
                    else
                    {
                        SelectedItem = null;
                        SelectedIndex = -1;
                    }
                    
                    StatusMessage = "選択アイテムを削除しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "削除中にエラー");
                StatusMessage = $"削除エラー: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            try
            {
                if (UndoStack.Count > 0)
                {
                    var action = UndoStack.Last();
                    UndoStack.Remove(action);
                    RedoStack.Add(action);
                    StatusMessage = "操作を元に戻しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo中にエラー");
                StatusMessage = $"Undoエラー: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            try
            {
                if (RedoStack.Count > 0)
                {
                    var action = RedoStack.Last();
                    RedoStack.Remove(action);
                    UndoStack.Add(action);
                    StatusMessage = "操作をやり直しました";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo中にエラー");
                StatusMessage = $"Redoエラー: {ex.Message}";
            }
        }

        private async Task LoadFileAsync()
        {
            try
            {
                StatusMessage = "ファイルを読み込み中...";
                // TODO: ファイル読み込み処理の実装
                await Task.Delay(100);
                StatusMessage = "ファイル読み込み完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
        }

        private async Task SaveFileAsync()
        {
            try
            {
                StatusMessage = "ファイルを保存中...";
                // TODO: ファイル保存処理の実装
                await Task.Delay(100);
                StatusMessage = "ファイル保存完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラー");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
        }

        private async Task ExecuteMacroAsync()
        {
            try
            {
                IsRunning = true;
                StatusMessage = "マクロ実行中...";
                
                // TODO: マクロ実行処理の実装
                await Task.Delay(1000);
                
                StatusMessage = "マクロ実行完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行中にエラー");
                StatusMessage = $"実行エラー: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void StopMacro()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                IsRunning = false;
                StatusMessage = "マクロ実行を停止しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止中にエラー");
                StatusMessage = $"停止エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            if (isRunning)
            {
                StatusMessage = "実行中...";
                _logger.LogDebug("実行状態に設定されました");
            }
            else
            {
                StatusMessage = "準備完了";
                CurrentExecutingItem = null;
                _logger.LogDebug("停止状態に設定されました");
            }
        }

        /// <summary>
        /// プログレスを初期化
        /// </summary>
        public void InitializeProgress()
        {
            Progress = 0;
            ExecutedItems = 0;
            ElapsedTime = TimeSpan.Zero;
            ExecutionLog.Clear();
            _logger.LogDebug("プログレスが初期化されました");
        }

        private int GetNextLineNumber()
        {
            return Items.Count > 0 ? Items.Max(x => x.LineNumber) + 1 : 1;
        }
    }
}