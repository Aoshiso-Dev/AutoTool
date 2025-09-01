using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using MacroPanels.List.Class;
using System.Windows;
using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Message;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Shapes;
using System.Security.Policy;
using System.ComponentModel;
using MacroPanels.ViewModel.Shared;
using Microsoft.Extensions.Logging;
using AutoTool.Services;
using AutoTool.Services.Performance;
using MacroPanels.ViewModel;
using MacroPanels.Message;
using MacroPanels.Model.MacroFactory;
using MacroPanels.Model.List.Interface;
using LogHelper;
using AutoTool.Model;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// ViewModelファクトリのインターフェース
    /// </summary>
    public interface IViewModelFactory
    {
        T Create<T>() where T : class;
        ButtonPanelViewModel CreateButtonPanelViewModel();
        ListPanelViewModel CreateListPanelViewModel();
        EditPanelViewModel CreateEditPanelViewModel();
        LogPanelViewModel CreateLogPanelViewModel();
        FavoritePanelViewModel CreateFavoritePanelViewModel();
    }

    /// <summary>
    /// メッセージサービスのインターフェース
    /// </summary>
    public interface IMessageService
    {
        void ShowError(string message, string title = "エラー");
        void ShowWarning(string message, string title = "警告");
        void ShowInformation(string message, string title = "情報");
        bool ShowConfirmation(string message, string title = "確認");
    }

    /// <summary>
    /// マクロパネルのViewModel（DI対応版）
    /// コマンド追加・削除・編集・移動機能をUndo/Redo対応で実装
    /// </summary>
    public partial class MacroPanelViewModel : ObservableObject
    {
        private readonly ILogger<MacroPanelViewModel> _logger;
        private readonly IViewModelFactory? _viewModelFactory;
        private readonly IMessageService? _messageService;
        private readonly IPerformanceService? _performanceService;
        private CancellationTokenSource? _cts;
        private CommandHistoryManager? _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ButtonPanelViewModel _buttonPanelViewModel;

        [ObservableProperty]
        private ListPanelViewModel _listPanelViewModel;

        [ObservableProperty]
        private EditPanelViewModel _editPanelViewModel;

        [ObservableProperty]
        private LogPanelViewModel _logPanelViewModel;

        [ObservableProperty]
        private FavoritePanelViewModel _favoritePanelViewModel;

        [ObservableProperty]
        private int _selectedListTabIndex = 0;

        [ObservableProperty]
        private string _executionStatus = "待機中";

        [ObservableProperty]
        private int _currentCommandIndex = 0;

        [ObservableProperty]
        private int _totalCommands = 0;

        // デフォルトコンストラクタ（既存コード用）
        [Obsolete("レガシーサポート用。新しいコードではDI対応コンストラクタを使用してください。")]
        public MacroPanelViewModel()
        {
            // NullLoggerを使用
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            _logger = loggerFactory.CreateLogger<MacroPanelViewModel>();
            
            // ViewModelを直接作成
            ListPanelViewModel = new ListPanelViewModel();
            var editLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EditPanelViewModel>.Instance;
            EditPanelViewModel = new EditPanelViewModel(editLogger);
            ButtonPanelViewModel = new ButtonPanelViewModel();
            LogPanelViewModel = new LogPanelViewModel();
            FavoritePanelViewModel = new FavoritePanelViewModel();
            
            _logger.LogInformation("MacroPanelViewModel: レガシーモードで初期化完了");
            
            RegisterMessages();
        }

        // DI対応コンストラクタ（推奨）
        public MacroPanelViewModel(
            ILogger<MacroPanelViewModel> logger,
            IViewModelFactory viewModelFactory,
            IMessageService messageService,
            IPerformanceService performanceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));

            _logger.LogInformation("MacroPanelViewModel: DI対応コンストラクタで初期化開始");
            
            InitializeViewModels();
            RegisterMessages();
            
            _logger.LogInformation("MacroPanelViewModel: DI対応で初期化完了");
        }

        private void InitializeViewModels()
        {
            try
            {
                _logger.LogDebug("ViewModelの初期化を開始します");

                if (_viewModelFactory != null)
                {
                    // DIファクトリを使用
                    ButtonPanelViewModel = _viewModelFactory.CreateButtonPanelViewModel();
                    ListPanelViewModel = _viewModelFactory.CreateListPanelViewModel();
                    EditPanelViewModel = _viewModelFactory.CreateEditPanelViewModel();
                    LogPanelViewModel = _viewModelFactory.CreateLogPanelViewModel();
                    FavoritePanelViewModel = _viewModelFactory.CreateFavoritePanelViewModel();
                }
                else
                {
                    // フォールバック：直接作成
                    _logger.LogWarning("ViewModelFactoryが利用できません。フォールバック作成を実行します");
                    ButtonPanelViewModel = new ButtonPanelViewModel();
                    ListPanelViewModel = new ListPanelViewModel();
                    var editLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EditPanelViewModel>.Instance;
                    EditPanelViewModel = new EditPanelViewModel(editLogger);
                    LogPanelViewModel = new LogPanelViewModel();
                    FavoritePanelViewModel = new FavoritePanelViewModel();
                }

                _logger.LogDebug("ViewModelの初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModelの初期化中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// CommandHistoryManagerを設定
        /// </summary>
        public void SetCommandHistory(CommandHistoryManager commandHistory)
        {
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _logger.LogDebug("CommandHistoryManagerが設定されました");
            
            // ListPanelViewModelにも設定
            ListPanelViewModel?.SetCommandHistory(commandHistory);
        }

        /// <summary>
        /// メッセージ登録（完全修正版）
        /// </summary>
        private void RegisterMessages()
        {
            try
            {
                _logger.LogDebug("メッセージ登録を開始します");

                // From ButtonPanelViewModel
                WeakReferenceMessenger.Default.Register<RunMessage>(this, async (sender, message) =>
                {
                    await PrepareAndRun();
                });
                
                WeakReferenceMessenger.Default.Register<StopMessage>(this, (sender, message) =>
                {
                    StopExecution();
                });
                
                WeakReferenceMessenger.Default.Register<SaveMessage>(this, (sender, message) =>
                {
                    ListPanelViewModel?.Save();
                    LogPanelViewModel?.WriteLog("ファイルを保存しました");
                });
                
                WeakReferenceMessenger.Default.Register<LoadMessage>(this, (sender, message) =>
                {
                    LoadCommands();
                });
                
                WeakReferenceMessenger.Default.Register<ClearMessage>(this, (sender, message) =>
                {
                    ClearAllCommands();
                });
                
                WeakReferenceMessenger.Default.Register<AddMessage>(this, (sender, message) =>
                {
                    var itemType = (message as AddMessage)?.ItemType;
                    if (itemType != null)
                    {
                        AddCommand(itemType);
                    }
                });
                
                WeakReferenceMessenger.Default.Register<UpMessage>(this, (sender, message) =>
                {
                    MoveCommandUp();
                });
                
                WeakReferenceMessenger.Default.Register<DownMessage>(this, (sender, message) =>
                {
                    MoveCommandDown();
                });
                
                WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (sender, message) =>
                {
                    DeleteSelectedCommand();
                });

                // From ListPanelViewModel
                WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (sender, message) =>
                {
                    var item = (message as ChangeSelectedMessage)?.Item;
                    EditPanelViewModel?.SetItem(item);
                });

                // From EditPanelViewModel
                WeakReferenceMessenger.Default.Register<EditCommandMessage>(this, (sender, message) =>
                {
                    var item = (message as EditCommandMessage)?.Item;
                    if (item != null)
                    {
                        EditCommand(item);
                    }
                });

                WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (sender, message) =>
                {
                    RefreshListPanel();
                });

                // From Commands - 実行状況の監視
                WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (sender, message) =>
                {
                    LogCommandStart(message as StartCommandMessage);
                });

                WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (sender, message) =>
                {
                    LogCommandFinish(message as FinishCommandMessage);
                });

                WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (sender, message) =>
                {
                    LogCommandProgress(message as DoingCommandMessage);
                });

                WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (sender, message) =>
                {
                    UpdateCommandProgress(message as UpdateProgressMessage);
                });

                // From Other
                WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
                {
                    var logText = (message as LogMessage)?.Text;
                    if (!string.IsNullOrEmpty(logText))
                    {
                        LogPanelViewModel?.WriteLog(logText);
                        GlobalLogger.Instance.Write(logText);
                    }
                });

                _logger.LogDebug("メッセージ登録が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メッセージ登録中にエラーが発生しました");
            }
        }

        #region コマンド操作メソッド

        /// <summary>
        /// コマンドを追加（Undo/Redo対応 + UI更新）
        /// </summary>
        /// <param name="itemType">追加するコマンドタイプ</param>
        public void AddCommand(string itemType)
        {
            try
            {
                _logger.LogDebug("コマンド追加: {ItemType}", itemType);

                // 追加操作をUndoスタックに追加
                if (_commandHistory != null && ListPanelViewModel != null)
                {
                    // 新しいアイテムを作成
                    var newItem = MacroPanels.Model.CommandDefinition.CommandRegistry.CreateCommandItem(itemType);
                    if (newItem != null)
                    {
                        var targetIndex = Math.Max(0, ListPanelViewModel.SelectedLineNumber + 1);
                        var addCommand = new AddItemCommand(
                            newItem,
                            targetIndex,
                            (item, index) => {
                                ListPanelViewModel.InsertAt(index, item);
                                RefreshAllPanels();
                            },
                            (index) => {
                                ListPanelViewModel.RemoveAt(index);
                                RefreshAllPanels();
                            }
                        );
                        _commandHistory.ExecuteCommand(addCommand);
                        
                        LogPanelViewModel?.WriteLog($"コマンド追加: {itemType} (位置: {targetIndex})");
                        _logger.LogInformation("コマンド追加完了: {ItemType} at {Index}", itemType, targetIndex);
                    }
                }
                else
                {
                    // フォールバック：直接追加
                    ListPanelViewModel?.Add(itemType);
                    RefreshAllPanels();
                    _logger.LogDebug("フォールバック: 直接コマンド追加 {ItemType}", itemType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド追加中にエラーが発生しました: {ItemType}", itemType);
                _messageService?.ShowError($"コマンドの追加に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 選択されたコマンドを削除（Undo/Redo対応 + UI更新）
        /// </summary>
        public void DeleteSelectedCommand()
        {
            try
            {
                if (ListPanelViewModel == null) return;

                var selectedItem = ListPanelViewModel.SelectedItem;
                var selectedIndex = ListPanelViewModel.SelectedLineNumber;

                if (selectedItem != null && _commandHistory != null)
                {
                    _logger.LogDebug("コマンド削除: インデックス {Index}", selectedIndex);

                    var removeCommand = new RemoveItemCommand(
                        selectedItem.Clone(),
                        selectedIndex,
                        (item, index) => {
                            ListPanelViewModel.InsertAt(index, item);
                            RefreshAllPanels();
                        },
                        (index) => {
                            ListPanelViewModel.RemoveAt(index);
                            RefreshAllPanels();
                        }
                    );
                    _commandHistory.ExecuteCommand(removeCommand);
                    
                    LogPanelViewModel?.WriteLog($"コマンド削除: インデックス {selectedIndex}");
                    _logger.LogInformation("コマンド削除完了: index {Index}", selectedIndex);
                }
                else
                {
                    // フォールバック：直接削除
                    ListPanelViewModel.Delete();
                    RefreshAllPanels();
                    _logger.LogDebug("フォールバック: 直接コマンド削除");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド削除中にエラーが発生しました");
                _messageService?.ShowError($"コマンドの削除に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 全コマンドをクリア（Undo/Redo対応 + UI更新）
        /// </summary>
        public void ClearAllCommands()
        {
            try
            {
                if (ListPanelViewModel == null) return;

                _logger.LogDebug("全コマンドクリア実行");

                // クリア操作をUndoスタックに追加
                if (_commandHistory != null)
                {
                    var currentItems = ListPanelViewModel.CommandList?.Items?.ToList();
                    if (currentItems != null && currentItems.Any())
                    {
                        var clearCommand = new ClearAllCommand(
                            currentItems,
                            () => {
                                ListPanelViewModel.Clear();
                                RefreshAllPanels();
                            },
                            (items) => {
                                RestoreItems(items);
                                RefreshAllPanels();
                            }
                        );
                        _commandHistory.ExecuteCommand(clearCommand);
                        
                        LogPanelViewModel?.WriteLog($"全コマンドクリア: {currentItems.Count}件削除");
                        _logger.LogInformation("全コマンドクリア完了: {Count}件", currentItems.Count);
                    }
                }
                else
                {
                    // フォールバック：直接クリア
                    ListPanelViewModel.Clear();
                    RefreshAllPanels();
                    _logger.LogDebug("フォールバック: 直接全クリア");
                }

                // ファイル読み込み後は履歴をクリア
                _commandHistory?.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全コマンドクリア中にエラーが発生しました");
                _messageService?.ShowError($"コマンドのクリアに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コマンドを上に移動（Undo/Redo対応 + UI更新）
        /// </summary>
        public void MoveCommandUp()
        {
            try
            {
                if (ListPanelViewModel == null) return;

                var fromIndex = ListPanelViewModel.SelectedLineNumber;
                var toIndex = fromIndex - 1;

                if (toIndex >= 0 && _commandHistory != null)
                {
                    _logger.LogDebug("コマンド上移動: {From} → {To}", fromIndex, toIndex);

                    var moveCommand = new MoveItemCommand(
                        fromIndex, toIndex,
                        (from, to) => {
                            ListPanelViewModel.MoveItem(from, to);
                            ListPanelViewModel.SelectedLineNumber = to;
                            RefreshListPanel();
                        }
                    );
                    _commandHistory.ExecuteCommand(moveCommand);
                    
                    LogPanelViewModel?.WriteLog($"コマンド上移動: {fromIndex} → {toIndex}");
                    _logger.LogInformation("コマンド上移動完了: {From} → {To}", fromIndex, toIndex);
                }
                else
                {
                    // フォールバック：直接移動
                    ListPanelViewModel.Up();
                    RefreshListPanel();
                    _logger.LogDebug("フォールバック: 直接上移動");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド上移動中にエラーが発生しました");
                _messageService?.ShowError($"コマンドの移動に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コマンドを下に移動（Undo/Redo対応 + UI更新）
        /// </summary>
        public void MoveCommandDown()
        {
            try
            {
                if (ListPanelViewModel == null) return;

                var fromIndex = ListPanelViewModel.SelectedLineNumber;
                var toIndex = fromIndex + 1;

                if (toIndex < ListPanelViewModel.GetCount() && _commandHistory != null)
                {
                    _logger.LogDebug("コマンド下移動: {From} → {To}", fromIndex, toIndex);

                    var moveCommand = new MoveItemCommand(
                        fromIndex, toIndex,
                        (from, to) => {
                            ListPanelViewModel.MoveItem(from, to);
                            ListPanelViewModel.SelectedLineNumber = to;
                            RefreshListPanel();
                        }
                    );
                    _commandHistory.ExecuteCommand(moveCommand);
                    
                    LogPanelViewModel?.WriteLog($"コマンド下移動: {fromIndex} → {toIndex}");
                    _logger.LogInformation("コマンド下移動完了: {From} → {To}", fromIndex, toIndex);
                }
                else
                {
                    // フォールバック：直接移動
                    ListPanelViewModel.Down();
                    RefreshListPanel();
                    _logger.LogDebug("フォールバック: 直接下移動");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド下移動中にエラーが発生しました");
                _messageService?.ShowError($"コマンドの移動に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コマンドを編集（Undo/Redo対応 + UI更新）
        /// </summary>
        /// <param name="item">編集されたアイテム</param>
        public void EditCommand(ICommandListItem item)
        {
            try
            {
                if (ListPanelViewModel == null || item == null) return;

                var oldItem = ListPanelViewModel.SelectedItem;
                var index = item.LineNumber - 1;

                // 編集操作をUndoスタックに追加
                if (oldItem != null && _commandHistory != null)
                {
                    _logger.LogDebug("コマンド編集: インデックス {Index}", index);

                    var editCommand = new EditItemCommand(
                        oldItem, item, index,
                        (editedItem, editIndex) => {
                            ListPanelViewModel.ReplaceAt(editIndex, editedItem);
                            RefreshListPanel();
                        }
                    );
                    _commandHistory.ExecuteCommand(editCommand);
                    
                    LogPanelViewModel?.WriteLog($"コマンド編集: インデックス {index}");
                    _logger.LogInformation("コマンド編集完了: index {Index}", index);
                }
                else
                {
                    // フォールバック：直接設定
                    ListPanelViewModel.SetSelectedItem(item);
                    ListPanelViewModel.SetSelectedLineNumber(item.LineNumber - 1);
                    RefreshListPanel();
                    _logger.LogDebug("フォールバック: 直接コマンド編集");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド編集中にエラーが発生しました");
                _messageService?.ShowError($"コマンドの編集に失敗しました: {ex.Message}");
            }
        }

        #endregion

        #region UI更新メソッド

        /// <summary>
        /// ListPanelの表示を強制更新
        /// </summary>
        private void RefreshListPanel()
        {
            try
            {
                ListPanelViewModel?.Refresh();
                _logger.LogDebug("ListPanelを更新しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanel更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 全パネルの表示を更新
        /// </summary>
        private void RefreshAllPanels()
        {
            try
            {
                RefreshListPanel();
                EditPanelViewModel?.SetListCount(ListPanelViewModel?.GetCount() ?? 0);
                
                // プロパティ変更通知を送信
                OnPropertyChanged(nameof(ListPanelViewModel));
                OnPropertyChanged(nameof(EditPanelViewModel));
                
                _logger.LogDebug("全パネルを更新しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "パネル更新中にエラーが発生しました");
            }
        }

        #endregion

        #region 実行・制御メソッド

        /// <summary>
        /// 実行準備とマクロ実行
        /// </summary>
        private async Task PrepareAndRun()
        {
            try
            {
                // 各ViewModelの準備
                ListPanelViewModel?.Prepare();
                EditPanelViewModel?.Prepare();
                LogPanelViewModel?.Prepare();
                FavoritePanelViewModel?.Prepare();
                ButtonPanelViewModel?.Prepare();

                // 実行状態に設定
                SetAllViewModelsRunningState(true);

                await Run();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行準備中にエラーが発生しました");
                _messageService?.ShowError($"実行準備に失敗しました: {ex.Message}");
                SetAllViewModelsRunningState(false);
            }
        }

        /// <summary>
        /// 実行停止
        /// </summary>
        private void StopExecution()
        {
            try
            {
                _logger.LogInformation("マクロ実行停止要求");
                
                _cts?.Cancel();
                SetAllViewModelsRunningState(false);
                
                LogPanelViewModel?.WriteLog("=== マクロ実行停止 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行停止中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 全ViewModelの実行状態を設定
        /// </summary>
        private void SetAllViewModelsRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            ExecutionStatus = isRunning ? "実行中" : "待機中";

            ButtonPanelViewModel?.SetRunningState(isRunning);
            EditPanelViewModel?.SetRunningState(isRunning);
            FavoritePanelViewModel?.SetRunningState(isRunning);
            ListPanelViewModel?.SetRunningState(isRunning);
            LogPanelViewModel?.SetRunningState(isRunning);
            
            _logger.LogDebug("全ViewModelの実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// メインのマクロ実行（修正版 - ファイル検証エラー対応）
        /// </summary>
        public async Task Run()
        {
            IEnumerable<ICommandListItem>? listItems = null;
            
            try
            {
                _logger.LogInformation("マクロ実行開始");
                
                // ListPanelViewModelとCommandListの存在確認を詳細に行う
                if (ListPanelViewModel == null)
                {
                    _logger.LogError("ListPanelViewModelがnullです");
                    _messageService?.ShowError("ListPanelViewModelが初期化されていません");
                    return;
                }

                if (ListPanelViewModel.CommandList == null)
                {
                    _logger.LogError("CommandListがnullです");
                    _messageService?.ShowError("CommandListが初期化されていません");
                    return;
                }

                listItems = ListPanelViewModel.CommandList.Items;
                if (listItems == null)
                {
                    _logger.LogError("CommandList.Itemsがnullです");
                    _messageService?.ShowError("コマンドリストのアイテムが初期化されていません");
                    return;
                }

                var commandArray = listItems.ToArray();
                _logger.LogDebug("コマンド数チェック: {Count}件", commandArray.Length);

                if (!commandArray.Any())
                {
                    _logger.LogWarning("実行するコマンドがありません（リストが空）");
                    _messageService?.ShowWarning("実行するコマンドがありません");
                    return;
                }

                // 各コマンドの詳細をログ出力（デバッグ用）
                for (int i = 0; i < commandArray.Length; i++)
                {
                    var cmd = commandArray[i];
                    _logger.LogDebug("コマンド[{Index}]: {Type}, LineNumber: {LineNumber}", 
                        i, cmd?.GetType().Name ?? "null", cmd?.LineNumber ?? -1);
                }

                var macro = MacroFactory.CreateMacro(commandArray) as LoopCommand;
                if (macro == null)
                {
                    _logger.LogError("MacroFactory.CreateMacroがnullを返しました");
                    _messageService?.ShowError("マクロの作成に失敗しました");
                    return;
                }

                SetAllViewModelsRunningState(true);
                _cts = new CancellationTokenSource();

                TotalCommands = commandArray.Length;
                CurrentCommandIndex = 0;

                _logger.LogInformation("マクロ実行開始: {TotalCommands}件のコマンドを実行します", TotalCommands);
                LogPanelViewModel?.WriteLog($"=== マクロ実行開始 ({TotalCommands}件) ===");

                var result = await macro.Execute(_cts.Token);

                if (result)
                {
                    _logger.LogInformation("マクロ実行完了");
                    LogPanelViewModel?.WriteLog("=== マクロ実行完了 ===");
                }
                else
                {
                    _logger.LogWarning("マクロ実行が失敗で終了しました");
                    LogPanelViewModel?.WriteLog("=== マクロ実行失敗 ===");
                    
                    // エラーの詳細を表示（LogPanelの最後の数行から取得）
                    var errorLines = LogPanelViewModel?.GetRecentErrorLines() ?? new List<string>();
                    if (errorLines.Any())
                    {
                        var errorMessage = string.Join("\n", errorLines.Take(3)); // 最新の3行まで
                        _messageService?.ShowError($"マクロ実行中にエラーが発生しました:\n\n{errorMessage}\n\n詳細はログパネルを確認してください。");
                    }
                    else
                    {
                        _messageService?.ShowError("マクロ実行中にエラーが発生しました。\n詳細はログパネルを確認してください。");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("マクロ実行がキャンセルされました");
                LogPanelViewModel?.WriteLog("=== マクロ実行キャンセル ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
                LogPanelViewModel?.WriteLog($"❌ 致命的エラー: {ex.Message}");
                
                if (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    // ファイル関連エラーの場合は詳細なメッセージを表示
                    string errorMessage;
                    if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    {
                        errorMessage = $"ファイル/フォルダが見つかりません:\n\n{ex.Message}\n\n" +
                                     "設定パネルでファイルパスを確認してください。";
                    }
                    else
                    {
                        errorMessage = $"マクロ実行中にエラーが発生しました:\n\n{ex.Message}";
                    }
                    
                    _messageService?.ShowError(errorMessage);
                }
            }
            finally
            {
                // 実行状態をリセット
                if (listItems != null)
                {
                    var runningItems = listItems.Where(x => x.IsRunning).ToList();
                    runningItems.ForEach(x => x.IsRunning = false);
                }
                
                var progressItems = ListPanelViewModel?.CommandList?.Items?.ToList();
                progressItems?.ForEach(x => x.Progress = 0);

                _cts?.Dispose();
                _cts = null;

                SetAllViewModelsRunningState(false);
                CurrentCommandIndex = 0;
            }
        }

        #endregion

        #region ログ・監視メソッド

        private void LogCommandStart(StartCommandMessage? message)
        {
            if (message?.Command == null) return;

            var command = message.Command;
            var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
            var commandName = command.GetType().ToString().Split('.').Last().Replace("Command", "").PadRight(20, ' ');

            var settingDict = command.Settings?.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(command.Settings, null));
            var logString = string.Empty;
            if (settingDict != null)
            {
                foreach (var setting in settingDict)
                {
                    logString += $"({setting.Key} = {setting.Value}), ";
                }
            }

            LogPanelViewModel?.WriteLog(lineNumber, commandName, logString);
            GlobalLogger.Instance.Write(lineNumber, commandName, logString);

            var commandItem = ListPanelViewModel?.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = true;
                CurrentCommandIndex = command.LineNumber;
            }
        }

        private void LogCommandFinish(FinishCommandMessage? message)
        {
            if (message?.Command == null) return;

            var command = message.Command;
            var commandItem = ListPanelViewModel?.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = false;
            }
        }

        private void LogCommandProgress(DoingCommandMessage? message)
        {
            if (message?.Command == null) return;

            var command = message.Command;
            var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
            var commandName = command.GetType().ToString().Split('.').Last().Replace("Command", "").PadRight(20, ' ');
            var detail = message.Detail;
            
            LogPanelViewModel?.WriteLog(lineNumber, commandName, detail);
            GlobalLogger.Instance.Write(lineNumber, commandName, detail);
        }

        private void UpdateCommandProgress(UpdateProgressMessage? message)
        {
            if (message?.Command == null) return;

            var command = message.Command;
            var progress = message.Progress;

            var commandItem = ListPanelViewModel?.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = progress;
            }
        }

        #endregion

        #region ファイル操作メソッド

        /// <summary>
        /// コマンドを読み込み
        /// </summary>
        private void LoadCommands()
        {
            try
            {
                ListPanelViewModel?.Load();
                RefreshAllPanels();

                // ファイル読み込み後は履歴をクリア
                _commandHistory?.Clear();
                
                var commandCount = ListPanelViewModel?.GetCount() ?? 0;
                LogPanelViewModel?.WriteLog($"コマンドファイル読み込み完了: {commandCount}件");
                _logger.LogInformation("コマンド読み込み完了: {Count}件", commandCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド読み込み中にエラーが発生しました");
                _messageService?.ShowError($"コマンドの読み込みに失敗しました: {ex.Message}");
            }
        }

        public void SetRunningState(bool isRunning)
        {
            SetAllViewModelsRunningState(isRunning);
        }

        public void SaveMacroFile(string filePath) => ListPanelViewModel?.Save(filePath);

        public void LoadMacroFile(string filePath)
        {
            ListPanelViewModel?.Load(filePath);
            RefreshAllPanels();
            
            // ファイル読み込み後は履歴をクリア
            _commandHistory?.Clear();
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// アイテムリストを復元（Undo用）
        /// </summary>
        private void RestoreItems(IEnumerable<ICommandListItem> items)
        {
            try
            {
                ListPanelViewModel?.Clear();
                foreach (var item in items)
                {
                    ListPanelViewModel?.AddItem(item.Clone());
                }
                
                _logger.LogDebug("アイテムリストを復元しました: {Count}件", items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム復元中にエラーが発生しました");
            }
        }

        #endregion
    }

    #region Legacy Support Classes

    /// <summary>
    /// レガシーサポート用ViewModelファクトリ
    /// </summary>
    internal class LegacyViewModelFactory : IViewModelFactory
    {
        private readonly ILogger _logger;

        public LegacyViewModelFactory(ILogger logger)
        {
            _logger = logger;
        }

        public T Create<T>() where T : class
        {
            _logger.LogDebug("レガシーファクトリでViewModel作成: {Type}", typeof(T).Name);
            
            if (typeof(T) == typeof(EditPanelViewModel))
            {
                var nullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EditPanelViewModel>.Instance;
                return new EditPanelViewModel(nullLogger) as T;
            }
            
            return Activator.CreateInstance<T>();
        }

        public ButtonPanelViewModel CreateButtonPanelViewModel() => new ButtonPanelViewModel();
        public ListPanelViewModel CreateListPanelViewModel() => new ListPanelViewModel();
        public EditPanelViewModel CreateEditPanelViewModel() => 
            new EditPanelViewModel(Microsoft.Extensions.Logging.Abstractions.NullLogger<EditPanelViewModel>.Instance);
        public LogPanelViewModel CreateLogPanelViewModel() => new LogPanelViewModel();
        public FavoritePanelViewModel CreateFavoritePanelViewModel() => new FavoritePanelViewModel();
    }

    /// <summary>
    /// レガシーサポート用メッセージサービス
    /// </summary>
    internal class LegacyMessageService : IMessageService
    {
        private readonly ILogger _logger;

        public LegacyMessageService(ILogger logger)
        {
            _logger = logger;
        }

        public void ShowError(string message, string title = "エラー")
        {
            _logger.LogError("Error: {Message}", message);
            System.Windows.MessageBox.Show(message, title, 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            _logger.LogWarning("Warning: {Message}", message);
            System.Windows.MessageBox.Show(message, title, 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "情報")
        {
            _logger.LogInformation("Info: {Message}", message);
            System.Windows.MessageBox.Show(message, title, 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title = "確認")
        {
            _logger.LogInformation("Confirmation: {Message}", message);
            var result = System.Windows.MessageBox.Show(message, title, 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Question);
            return result == System.Windows.MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// レガシーサポート用パフォーマンスサービス
    /// </summary>
    internal class LegacyPerformanceService : IPerformanceService
    {
        private readonly ILogger _logger;

        public LegacyPerformanceService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await operation();
                stopwatch.Stop();
                _logger.LogDebug("Operation completed: {Operation} ({ElapsedMs}ms)", operationName, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Operation failed: {Operation} ({ElapsedMs}ms)", operationName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task MeasureAsync(string operationName, Func<Task> operation)
        {
            await MeasureAsync(operationName, async () =>
            {
                await operation();
                return Task.CompletedTask;
            });
        }

        public PerformanceStatistics GetStatistics()
        {
            return new PerformanceStatistics();
        }

        public void RecordMetric(string name, double value, Dictionary<string, string>? tags = null)
        {
            _logger.LogDebug("Metric recorded: {Name} = {Value}", name, value);
        }

        public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
        {
            _logger.LogDebug("Counter incremented: {Name}", name);
        }

        public void ClearMetrics()
        {
            _logger.LogDebug("Metrics cleared");
        }

        public void Reset()
        {
            _logger.LogDebug("Performance statistics reset");
        }
    }

    #endregion
}
