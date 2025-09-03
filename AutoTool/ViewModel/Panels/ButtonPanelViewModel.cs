using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合版：ButtonPanelViewModel（高度なコマンド操作）
    /// </summary>
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _recentCommands = new();

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _favoriteCommands = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _filteredItemTypes = new();

        [ObservableProperty]
        private string _selectedCategory = "すべて";

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private bool _showAdvancedOptions = false;

        [ObservableProperty]
        private int _batchAddCount = 1;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 5統合版ButtonPanelViewModel を初期化しています");
            
            InitializeItemTypes();
            LoadRecentCommands();
            LoadFavoriteCommands();
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("アイテムタイプの初期化を開始します");

                // CommandRegistryを初期化
                CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistry.Initialize() 完了");

                var orderedTypeNames = CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("GetOrderedTypeNames()で取得した型名数: {Count}", orderedTypeNames?.Count() ?? 0);

                var displayItems = orderedTypeNames?
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList() ?? new List<CommandDisplayItem>();
                
                _logger.LogDebug("作成したdisplayItems数: {Count}", displayItems.Count);
                
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                FilteredItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();
                
                // カテゴリーリストを作成
                var categories = displayItems.Select(item => item.Category).Distinct().OrderBy(c => c).ToList();
                categories.Insert(0, "すべて");
                Categories = new ObservableCollection<string>(categories);
                
                _logger.LogDebug("アイテムタイプの初期化が完了しました: {Count}個", ItemTypes.Count);
                _logger.LogDebug("選択されたアイテム: {DisplayName}", SelectedItemType?.DisplayName ?? "なし");
                
                StatusMessage = $"{ItemTypes.Count}個のコマンドが利用可能です";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプの初期化中にエラーが発生しました");
                StatusMessage = $"初期化エラー: {ex.Message}";
                throw;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterCommands();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            FilterCommands();
        }

        private void FilterCommands()
        {
            try
            {
                var filtered = ItemTypes.AsEnumerable();

                // カテゴリーフィルター
                if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "すべて")
                {
                    filtered = filtered.Where(item => item.Category == SelectedCategory);
                }

                // テキスト検索フィルター
                if (!string.IsNullOrEmpty(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(item => 
                        item.DisplayName.ToLower().Contains(searchLower) ||
                        item.TypeName.ToLower().Contains(searchLower) ||
                        item.Category.ToLower().Contains(searchLower));
                }

                FilteredItemTypes = new ObservableCollection<CommandDisplayItem>(filtered);
                
                // 検索結果がある場合は最初のアイテムを選択
                if (FilteredItemTypes.Count > 0 && !FilteredItemTypes.Contains(SelectedItemType))
                {
                    SelectedItemType = FilteredItemTypes.First();
                }

                StatusMessage = $"{FilteredItemTypes.Count}個のコマンドが表示されています";
                _logger.LogDebug("コマンドフィルター適用: {Count}個表示", FilteredItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドフィルター中にエラーが発生しました");
                StatusMessage = $"フィルターエラー: {ex.Message}";
            }
        }

        #region コマンド操作

        [RelayCommand]
        public void Add() 
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("追加コマンドを送信します: {ItemType}", SelectedItemType.TypeName);
                    
                    // バッチ追加対応
                    for (int i = 0; i < BatchAddCount; i++)
                    {
                        WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType.TypeName));
                    }
                    
                    // 最近使用したコマンドに追加
                    AddToRecentCommands(SelectedItemType);
                    
                    StatusMessage = BatchAddCount > 1 
                        ? $"{SelectedItemType.DisplayName}を{BatchAddCount}個追加しました"
                        : $"{SelectedItemType.DisplayName}を追加しました";
                }
                else
                {
                    _logger.LogWarning("アイテムタイプが選択されていません");
                    StatusMessage = "コマンドが選択されていません";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "追加コマンドの処理中にエラーが発生しました");
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddFromRecent(CommandDisplayItem recentCommand)
        {
            try
            {
                if (recentCommand != null)
                {
                    WeakReferenceMessenger.Default.Send(new AddMessage(recentCommand.TypeName));
                    StatusMessage = $"{recentCommand.DisplayName}を追加しました（最近使用）";
                    _logger.LogDebug("最近使用したコマンドから追加: {ItemType}", recentCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用コマンド追加中にエラーが発生しました");
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddFromFavorite(CommandDisplayItem favoriteCommand)
        {
            try
            {
                if (favoriteCommand != null)
                {
                    WeakReferenceMessenger.Default.Send(new AddMessage(favoriteCommand.TypeName));
                    StatusMessage = $"{favoriteCommand.DisplayName}を追加しました（お気に入り）";
                    _logger.LogDebug("お気に入りコマンドから追加: {ItemType}", favoriteCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "お気に入りコマンド追加中にエラーが発生しました");
                StatusMessage = $"追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddToFavorites()
        {
            try
            {
                if (SelectedItemType != null && !FavoriteCommands.Any(f => f.TypeName == SelectedItemType.TypeName))
                {
                    FavoriteCommands.Add(SelectedItemType);
                    SaveFavoriteCommands();
                    StatusMessage = $"{SelectedItemType.DisplayName}をお気に入りに追加しました";
                    _logger.LogDebug("お気に入りに追加: {ItemType}", SelectedItemType.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "お気に入り追加中にエラーが発生しました");
                StatusMessage = $"お気に入り追加エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void RemoveFromFavorites(CommandDisplayItem favoriteCommand)
        {
            try
            {
                if (favoriteCommand != null)
                {
                    FavoriteCommands.Remove(favoriteCommand);
                    SaveFavoriteCommands();
                    StatusMessage = $"{favoriteCommand.DisplayName}をお気に入りから削除しました";
                    _logger.LogDebug("お気に入りから削除: {ItemType}", favoriteCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "お気に入り削除中にエラーが発生しました");
                StatusMessage = $"お気に入り削除エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Run()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("停止コマンドを送信します");
                    WeakReferenceMessenger.Default.Send(new StopMessage());
                    StatusMessage = "マクロを停止しています...";
                }
                else
                {
                    _logger.LogInformation("実行コマンドを送信します");
                    WeakReferenceMessenger.Default.Send(new RunMessage());
                    StatusMessage = "マクロを実行しています...";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行/停止コマンドの処理中にエラーが発生しました");
                StatusMessage = $"実行エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Save() 
        {
            try
            {
                _logger.LogDebug("保存コマンドを送信します");
                WeakReferenceMessenger.Default.Send(new SaveMessage());
                StatusMessage = "ファイルを保存しています...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存コマンドの処理中にエラーが発生しました");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Load() 
        {
            try
            {
                _logger.LogDebug("読み込みコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new LoadMessage());
                StatusMessage = "ファイルを読み込んでいます...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "読み込みコマンドの処理中にエラーが発生しました");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Clear() 
        {
            try
            {
                _logger.LogDebug("クリアコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new ClearMessage());
                StatusMessage = "コマンドリストをクリアしています...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリアコマンドの処理中にエラーが発生しました");
                StatusMessage = $"クリアエラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Up() 
        {
            try
            {
                _logger.LogDebug("上移動コマンドを送信します");
                WeakReferenceMessenger.Default.Send(new UpMessage());
                StatusMessage = "選択項目を上に移動しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上移動コマンドの処理中にエラーが発生しました");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Down() 
        {
            try
            {
                _logger.LogDebug("下移動コマンドを送信します");
                WeakReferenceMessenger.Default.Send(new DownMessage());
                StatusMessage = "選択項目を下に移動しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下移動コマンドの処理中にエラーが発生しました");
                StatusMessage = $"移動エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Delete() 
        {
            try
            {
                _logger.LogDebug("削除コマンドを送信します");
                WeakReferenceMessenger.Default.Send(new DeleteMessage());
                StatusMessage = "選択項目を削除しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "削除コマンドの処理中にエラーが発生しました");
                StatusMessage = $"削除エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Undo() 
        {
            try
            {
                _logger.LogDebug("元に戻すコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new UndoMessage());
                StatusMessage = "操作を元に戻しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "元に戻すコマンドの処理中にエラーが発生しました");
                StatusMessage = $"Undoエラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Redo() 
        {
            try
            {
                _logger.LogDebug("やり直しコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new RedoMessage());
                StatusMessage = "操作をやり直しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "やり直しコマンドの処理中にエラーが発生しました");
                StatusMessage = $"Redoエラー: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ToggleAdvancedOptions()
        {
            ShowAdvancedOptions = !ShowAdvancedOptions;
            StatusMessage = ShowAdvancedOptions ? "高度なオプションを表示" : "高度なオプションを非表示";
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedCategory = "すべて";
            StatusMessage = "検索条件をクリアしました";
        }

        #endregion

        #region 最近使用・お気に入り管理

        private void AddToRecentCommands(CommandDisplayItem command)
        {
            try
            {
                // 既存のアイテムを削除
                var existing = RecentCommands.FirstOrDefault(r => r.TypeName == command.TypeName);
                if (existing != null)
                {
                    RecentCommands.Remove(existing);
                }

                // 先頭に追加
                RecentCommands.Insert(0, command);

                // 最大10個まで保持
                while (RecentCommands.Count > 10)
                {
                    RecentCommands.RemoveAt(RecentCommands.Count - 1);
                }

                SaveRecentCommands();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用コマンド追加中にエラーが発生しました");
            }
        }

        private void LoadRecentCommands()
        {
            try
            {
                // TODO: 設定ファイルから読み込み
                // 現在は空で初期化
                RecentCommands = new ObservableCollection<CommandDisplayItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用コマンド読み込み中にエラーが発生しました");
                RecentCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        private void SaveRecentCommands()
        {
            try
            {
                // TODO: 設定ファイルに保存
                _logger.LogDebug("最近使用コマンドを保存しました: {Count}件", RecentCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用コマンド保存中にエラーが発生しました");
            }
        }

        private void LoadFavoriteCommands()
        {
            try
            {
                // TODO: 設定ファイルから読み込み
                // 現在は空で初期化
                FavoriteCommands = new ObservableCollection<CommandDisplayItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "お気に入りコマンド読み込み中にエラーが発生しました");
                FavoriteCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        private void SaveFavoriteCommands()
        {
            try
            {
                // TODO: 設定ファイルに保存
                _logger.LogDebug("お気に入りコマンドを保存しました: {Count}件", FavoriteCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "お気に入りコマンド保存中にエラーが発生しました");
            }
        }

        #endregion

        #region その他

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "マクロ実行中..." : "準備完了";
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        public void Prepare() 
        {
            _logger.LogDebug("ButtonPanelViewModel の準備を実行します");
            StatusMessage = "準備完了";
        }

        /// <summary>
        /// 利用可能なコマンド統計を取得
        /// </summary>
        public CommandTypeStats GetCommandTypeStats()
        {
            return new CommandTypeStats
            {
                TotalTypes = ItemTypes.Count,
                CategoryStats = ItemTypes.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count()),
                RecentCount = RecentCommands.Count,
                FavoriteCount = FavoriteCommands.Count
            };
        }

        #endregion
    }

    #region 補助クラス

    /// <summary>
    /// コマンドタイプ統計
    /// </summary>
    public class CommandTypeStats
    {
        public int TotalTypes { get; set; }
        public Dictionary<string, int> CategoryStats { get; set; } = new();
        public int RecentCount { get; set; }
        public int FavoriteCount { get; set; }
    }

    #endregion
}