using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.ViewModel.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTool.Command.Definition;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5完全統合：ButtonPanelViewModel（高次なコマンド操作）
    /// </summary>
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;

        private readonly Guid _instanceId = Guid.NewGuid();

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

        [ObservableProperty]
        private string _currentCommandDescription = string.Empty;

        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 5完全統合：ButtonPanelViewModel が初期化されています (Instance={InstanceId})", _instanceId);

            // メッセンジャーの状態を確認
            var messengerInfo = WeakReferenceMessenger.Default.ToString();
            _logger.LogDebug("使用中のMessenger: {MessengerInfo}", messengerInfo);

            // キーボードショートカットのメッセージ登録
            SetupKeyboardShortcuts();

            InitializeItemTypes();
            LoadRecentCommands();
            LoadFavoriteCommands();
        }

        /// <summary>
        /// キーボードショートカットのメッセージ処理を設定
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            try
            {
                // 一時的にコメントアウト - メッセージング実装後に再有効化
                /*
                // Ctrl+S: 保存
                WeakReferenceMessenger.Default.Register<KeyboardShortcutMessage>(this, (r, m) =>
                {
                    if (m.Key == "Ctrl+S")
                    {
                        SaveCommand.Execute(null);
                    }
                    else if (m.Key == "Ctrl+Z")
                    {
                        UndoCommand.Execute(null);
                    }
                    else if (m.Key == "Ctrl+Y")
                    {
                        RedoCommand.Execute(null);
                    }
                });
                */

                _logger.LogDebug("キーボードショートカット設定完了（一時的に無効化中）");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キーボードショートカット設定中にエラー");
            }
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("アイテムタイプの初期化を開始します");

                // DirectCommandRegistryの初期化 - serviceProviderがnullでも基本コマンドタイプは取得可能
                AutoToolCommandRegistry.Initialize(null); // 明示的に初期化を呼び出す

                var orderedTypeNames = AutoToolCommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("GetOrderedTypeNames()で取得したタイプ数: {Count}", orderedTypeNames?.Count() ?? 0);

                if (orderedTypeNames != null)
                {
                    foreach (var typeName in orderedTypeNames)
                    {
                        _logger.LogTrace("取得されたコマンドタイプ: {TypeName}", typeName);
                    }
                }

                var displayItems = orderedTypeNames?
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = AutoToolCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList() ?? new List<CommandDisplayItem>();

                _logger.LogDebug("作成されたdisplayItems数: {Count}", displayItems.Count);

                // 作成されたアイテムの詳細をログ出力
                foreach (var item in displayItems.Take(5)) // 最初の5個のみ
                {
                    _logger.LogDebug("作成されたCommandDisplayItem: TypeName={TypeName}, DisplayName={DisplayName}, Category={Category}",
                        item.TypeName, item.DisplayName, item.Category);
                }

                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                FilteredItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();

                _logger.LogDebug("SelectedItemType設定: {SelectedItemType}", SelectedItemType?.TypeName ?? "null");

                // カテゴリーリストを作成
                var categories = displayItems.Select(item => item.Category).Distinct().OrderBy(c => c).ToList();
                categories.Insert(0, "すべて");
                Categories = new ObservableCollection<string>(categories);

                _logger.LogDebug("アイテムタイプの初期化が完了しました: {Count}個", ItemTypes.Count);
                _logger.LogDebug("選択されたアイテム: {DisplayName}", SelectedItemType?.DisplayName ?? "なし");

                StatusMessage = $"{ItemTypes.Count}のコマンドが利用可能です";
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
                if (FilteredItemTypes.Count > 0)
                {
                    if (!FilteredItemTypes.Contains(SelectedItemType))
                    {
                        SelectedItemType = FilteredItemTypes.First();
                        _logger.LogDebug("フィルター後の最初のアイテムを選択: {TypeName}", SelectedItemType.TypeName);
                    }
                }
                else
                {
                    // フィルター結果がない場合はnullに設定
                    SelectedItemType = null;
                    _logger.LogDebug("フィルター結果なし、SelectedItemTypeをnullに設定");
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

        partial void OnStatusMessageChanged(string value)
        {
            OnPropertyChanged(nameof(StatusText));
        }

        partial void OnSelectedItemTypeChanged(CommandDisplayItem? value)
        {
            _logger.LogDebug("SelectedItemType変更: {OldValue} -> {NewValue}",
                _selectedItemType?.TypeName ?? "null", value?.TypeName ?? "null");
        }

        #region コマンド操作

        [RelayCommand]
        public void Add()
        {
            try
            {
                _logger.LogDebug("Add()メソッド開始 - SelectedItemType: {SelectedItemType}", SelectedItemType?.TypeName ?? "null");

                if (SelectedItemType != null)
                {
                    _logger.LogInformation("追加コマンドを送信します: {ItemType} (DisplayName: {DisplayName})",
                        SelectedItemType.TypeName, SelectedItemType.DisplayName);

                    // メッセンジャーの受信者数を確認
                    try
                    {
                        // リフレクションを使ってReceiver数を確認
                        var messengerType = WeakReferenceMessenger.Default.GetType();
                        var recipientsField = messengerType.GetField("recipients",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (recipientsField?.GetValue(WeakReferenceMessenger.Default) is System.Collections.IDictionary recipients)
                        {
                            _logger.LogDebug("Messenger受信者数: {Count}", recipients.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Messengerの受信者数確認に失敗");
                    }

                    // バッチ追加対応
                    for (int i = 0; i < BatchAddCount; i++)
                    {
                        var addMessage = new AddMessage(SelectedItemType.TypeName);
                        _logger.LogDebug("AddMessage送信中: {ItemType} (#{Index})", SelectedItemType.TypeName, i + 1);
                        WeakReferenceMessenger.Default.Send(addMessage);

                        // 送信後の確認
                        _logger.LogDebug("AddMessage送信完了: {ItemType} (#{Index})", SelectedItemType.TypeName, i + 1);
                    }

                    // 最近使用したコマンドに追加
                    AddToRecentCommands(SelectedItemType);

                    StatusMessage = BatchAddCount > 1
                        ? $"{SelectedItemType.DisplayName}を{BatchAddCount}個追加しました"
                        : $"{SelectedItemType.DisplayName}を追加しました";

                    _logger.LogInformation("コマンド追加完了: {ItemType} x {Count}", SelectedItemType.TypeName, BatchAddCount);
                }
                else
                {
                    _logger.LogWarning("アイテムタイプが選択されていません");
                    StatusMessage = "コマンドが選択されていません";

                    // デバッグ用：利用可能なアイテムの状況を確認
                    _logger.LogDebug("利用可能なアイテム数: ItemTypes={ItemTypesCount}, FilteredItemTypes={FilteredCount}",
                        ItemTypes.Count, FilteredItemTypes.Count);

                    if (ItemTypes.Count > 0)
                    {
                        var firstItem = ItemTypes.First();
                        _logger.LogDebug("最初のアイテム: {TypeName} ({DisplayName})", firstItem.TypeName, firstItem.DisplayName);

                        // 強制的に最初のアイテムを選択してテスト
                        SelectedItemType = firstItem;
                        _logger.LogDebug("最初のアイテムを強制選択してリトライ");
                        StatusMessage = "最初のアイテムを選択しました";
                    }
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
                    _logger.LogInformation("停止コマンドを送信します (ButtonPanel -> MainWindow統合)");
                    WeakReferenceMessenger.Default.Send(new StopMessage());
                    StatusMessage = "マクロを停止しています...";
                }
                else
                {
                    _logger.LogInformation("実行コマンドを送信します (ButtonPanel -> MainWindow統合)");
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
                _logger.LogDebug("保存コマンドを送信します (ButtonPanel -> MainWindow統合)");
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
                _logger.LogDebug("読み込みコマンドを送信します (ButtonPanel -> MainWindow統合)");
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

        [RelayCommand]
        public void TestAddMessage()
        {
            try
            {
                _logger.LogInformation("テスト用AddMessage送信開始");
                var testMessage = new AddMessage("Click");
                WeakReferenceMessenger.Default.Send(testMessage);
                _logger.LogInformation("テスト用AddMessage送信完了: Click");
                StatusMessage = "テストメッセージを送信しました (Click)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テストメッセージ送信中にエラー");
                StatusMessage = $"テストエラー: {ex.Message}";
            }
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

        /// <summary>
        /// StatusTextプロパティ（MainWindowViewModelから参照用）
        /// </summary>
        public string StatusText => StatusMessage;

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
}