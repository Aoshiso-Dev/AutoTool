using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Message;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;
using AutoTool.Model.MacroFactory;
using AutoTool.Services.Plugin;
using System.Windows;
using System.Linq;
using AutoTool.Command.Interface;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// メインウィンドウのボタン機能を管理するサービス
    /// </summary>
    public interface IMainWindowButtonService
    {
        // 実行制御
        IRelayCommand RunMacroCommand { get; }
        bool IsRunning { get; }
        bool CanRunMacro { get; }
        bool CanStopMacro { get; }
        
        // リスト操作
        IRelayCommand AddCommandCommand { get; }
        IRelayCommand DeleteCommandCommand { get; }
        IRelayCommand UpCommandCommand { get; }
        IRelayCommand DownCommandCommand { get; }
        IRelayCommand ClearCommandCommand { get; }
        
        // 履歴操作
        IRelayCommand UndoCommand { get; }
        IRelayCommand RedoCommand { get; }
        
        // デバッグ・テスト
        IRelayCommand AddTestCommandCommand { get; }
        IRelayCommand TestExecutionHighlightCommand { get; }
        
        // プロパティ
        CommandDisplayItem? SelectedItemType { get; set; }
        int CommandCount { get; }
        
        // イベント
        event EventHandler<bool> RunningStateChanged;
        event EventHandler<string> StatusChanged;
        event EventHandler<int> CommandCountChanged;
        
        // メソッド
        void UpdateCommandCount(int count);
        void SetSelectedItemType(CommandDisplayItem? itemType);
    }

    /// <summary>
    /// メインウィンドウのボタン機能サービス実装
    /// </summary>
    public partial class MainWindowButtonService : IMainWindowButtonService
    {
        private readonly ILogger<MainWindowButtonService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;
        private readonly IMessenger _messenger;
        
        // マクロ実行関連
        private CancellationTokenSource? _currentCancellationTokenSource;
        private bool _isRunning = false;
        private int _commandCount = 0;
        private CommandDisplayItem? _selectedItemType;

        // Command properties
        public IRelayCommand RunMacroCommand { get; }
        public IRelayCommand AddCommandCommand { get; }
        public IRelayCommand DeleteCommandCommand { get; }
        public IRelayCommand UpCommandCommand { get; }
        public IRelayCommand DownCommandCommand { get; }
        public IRelayCommand ClearCommandCommand { get; }
        public IRelayCommand UndoCommand { get; }
        public IRelayCommand RedoCommand { get; }
        public IRelayCommand AddTestCommandCommand { get; }
        public IRelayCommand TestExecutionHighlightCommand { get; }

        // Properties
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    RunningStateChanged?.Invoke(this, value);
                    
                    // CanExecute状態を更新
                    ((RelayCommand)RunMacroCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)AddCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)DeleteCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)UpCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)DownCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ClearCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)UndoCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)RedoCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)AddTestCommandCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)TestExecutionHighlightCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanRunMacro => !IsRunning && CommandCount > 0;
        public bool CanStopMacro => IsRunning;

        public CommandDisplayItem? SelectedItemType
        {
            get => _selectedItemType;
            set
            {
                _selectedItemType = value;
                ((RelayCommand)AddCommandCommand).NotifyCanExecuteChanged();
            }
        }

        public int CommandCount
        {
            get => _commandCount;
            private set
            {
                if (_commandCount != value)
                {
                    _commandCount = value;
                    CommandCountChanged?.Invoke(this, value);
                    
                    // 実行可能状態を更新
                    ((RelayCommand)RunMacroCommand).NotifyCanExecuteChanged();
                }
            }
        }

        // Events
        public event EventHandler<bool>? RunningStateChanged;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<int>? CommandCountChanged;

        public MainWindowButtonService(
            ILogger<MainWindowButtonService> logger,
            IServiceProvider serviceProvider,
            IPluginService pluginService,
            IMessenger messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            // Initialize commands
            RunMacroCommand = new RelayCommand(ExecuteRunMacro, () => CanExecuteRunMacro());
            AddCommandCommand = new RelayCommand(ExecuteAddCommand, () => !IsRunning && SelectedItemType != null);
            DeleteCommandCommand = new RelayCommand(ExecuteDeleteCommand, () => !IsRunning);
            UpCommandCommand = new RelayCommand(ExecuteUpCommand, () => !IsRunning);
            DownCommandCommand = new RelayCommand(ExecuteDownCommand, () => !IsRunning);
            ClearCommandCommand = new RelayCommand(ExecuteClearCommand, () => !IsRunning);
            UndoCommand = new RelayCommand(ExecuteUndoCommand, () => !IsRunning);
            RedoCommand = new RelayCommand(ExecuteRedoCommand, () => !IsRunning);
            AddTestCommandCommand = new RelayCommand(ExecuteAddTestCommand, () => !IsRunning);
            TestExecutionHighlightCommand = new RelayCommand(ExecuteTestExecutionHighlight, () => !IsRunning);

            SetupMessaging();
        }

        public void UpdateCommandCount(int count)
        {
            CommandCount = count;
        }

        public void SetSelectedItemType(CommandDisplayItem? itemType)
        {
            SelectedItemType = itemType;
        }

        private bool CanExecuteRunMacro()
        {
            return IsRunning || (!IsRunning && CommandCount > 0);
        }

        private void ExecuteRunMacro()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("停止要求を送信します");
                    StatusChanged?.Invoke(this, "停止要求を送信しました");
                    
                    // 停止処理を別タスクで実行（UIをブロックしない）
                    _ = Task.Run(() => StopMacroInternal());
                }
                else
                {
                    _logger.LogInformation("実行要求を開始します");
                    StatusChanged?.Invoke(this, "実行準備中...");
                    
                    // 非同期でマクロ実行を開始（UIスレッドをブロックしない）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await StartMacroAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "バックグラウンドマクロ実行中にエラー");
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                StatusChanged?.Invoke(this, $"実行エラー: {ex.Message}");
                                IsRunning = false;
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroCommand 実行中にエラー");
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusChanged?.Invoke(this, $"実行エラー: {ex.Message}");
                    IsRunning = false;
                });
            }
        }

        private void ExecuteAddCommand()
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("追加要求: {Type}", SelectedItemType.TypeName);
                    _messenger.Send(new AddMessage(SelectedItemType.TypeName));
                }
                else
                {
                    _logger.LogWarning("追加要求: SelectedItemType が null です");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCommand 実行中にエラー");
            }
        }

        private void ExecuteDeleteCommand()
        {
            try
            {
                _logger.LogDebug("削除要求を送信");
                _messenger.Send(new DeleteMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCommand 実行中にエラー");
            }
        }

        private void ExecuteUpCommand()
        {
            try
            {
                _logger.LogDebug("上移動要求を送信");
                _messenger.Send(new UpMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpCommand 実行中にエラー");
            }
        }

        private void ExecuteDownCommand()
        {
            try
            {
                _logger.LogDebug("下移動要求を送信");
                _messenger.Send(new DownMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownCommand 実行中にエラー");
            }
        }

        private void ExecuteClearCommand()
        {
            try
            {
                _logger.LogDebug("クリア要求を送信");
                _messenger.Send(new ClearMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCommand 実行中にエラー");
            }
        }

        private void ExecuteUndoCommand()
        {
            try
            {
                _logger.LogDebug("Undo要求を送信");
                _messenger.Send(new UndoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UndoCommand 実行中にエラー");
            }
        }

        private void ExecuteRedoCommand()
        {
            try
            {
                _logger.LogDebug("Redo要求を送信");
                _messenger.Send(new RedoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedoCommand 実行中にエラー");
            }
        }

        private void ExecuteAddTestCommand()
        {
            try
            {
                _logger.LogDebug("テストコマンド追加要求送信（動的UI版）");
                
                // 動的UIのUniversalCommandItemを作成
                var testItem = AutoTool.Model.CommandDefinition.DirectCommandRegistry.CreateUniversalItem("Test");
                
                // AddUniversalItemMessageを送信
                _messenger.Send(new AddUniversalItemMessage(testItem));
                
                StatusChanged?.Invoke(this, "動的テストコマンドを追加しました");
                _logger.LogInformation("動的TestCommandが正常に追加されました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的テストコマンド追加でエラーが発生しました");
                StatusChanged?.Invoke(this, $"動的テストコマンド追加エラー: {ex.Message}");
            }
        }

        private void ExecuteTestExecutionHighlight()
        {
            try
            {
                _logger.LogDebug("実行ハイライトテスト開始");
                
                // DIからListPanelViewModelを取得
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel == null)
                {
                    StatusChanged?.Invoke(this, "ListPanelViewModelが見つかりません");
                    return;
                }

                if (listPanelViewModel.Items.Count == 0)
                {
                    StatusChanged?.Invoke(this, "テスト対象のコマンドがありません");
                    return;
                }

                StatusChanged?.Invoke(this, "実行ハイライトテスト開始");

                // 最初のアイテムを実行中状態にする
                var firstItem = listPanelViewModel.Items.First();
                
                // UIスレッドで実行状態を設定
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    firstItem.IsRunning = true;
                    firstItem.Progress = 0;
                    listPanelViewModel.CurrentExecutingItem = firstItem;
                });

                // プログレスを段階的に更新
                Task.Run(async () =>
                {
                    for (int i = 0; i <= 100; i += 10)
                    {
                        await Task.Delay(500); // 500msごとに更新
                        
                        // UIスレッドで実行
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            firstItem.Progress = i;
                        });
                    }

                    // 完了状態に設定
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        firstItem.IsRunning = false;
                        firstItem.Progress = 100;
                        listPanelViewModel.CurrentExecutingItem = null;
                        StatusChanged?.Invoke(this, "実行ハイライトテスト完了");

                        // 少し待ってからプログレスをリセット
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                firstItem.Progress = 0;
                            });
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行ハイライトテスト中にエラーが発生しました");
                StatusChanged?.Invoke(this, $"実行ハイライトテストエラー: {ex.Message}");
            }
        }

        private void StopMacroInternal()
        {
            try
            {
                _logger.LogInformation("停止要求を受信しました");
                
                if (_currentCancellationTokenSource != null && !_currentCancellationTokenSource.IsCancellationRequested)
                {
                    _logger.LogInformation("キャンセル要求を送信しました");
                    _currentCancellationTokenSource.Cancel();
                    
                    // UI状態を即座に更新
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusChanged?.Invoke(this, "停止処理中...");
                    });
                }
                else
                {
                    _logger.LogWarning("キャンセル要求: 既にキャンセル済みかトークンソースがnull");
                    
                    // 状態が不正な場合は強制リセット
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (IsRunning)
                        {
                            IsRunning = false;
                            StatusChanged?.Invoke(this, "強制停止完了");
                            
                            // ListPanelの状態もリセット
                            var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                            listPanelViewModel?.SetRunningState(false);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止処理中にエラー");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusChanged?.Invoke(this, $"停止エラー: {ex.Message}");
                    IsRunning = false;
                });
            }
        }

        private async Task StartMacroAsync()
        {
            try
            {
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel == null)
                {
                    _logger.LogError("ListPanelViewModel が見つかりません。実行を中止します。");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "実行エラー: ListPanel VM 取得失敗");
                    });
                    return;
                }

                if (IsRunning)
                {
                    _logger.LogWarning("既に実行中のため開始しません");
                    return;
                }
                
                if (listPanelViewModel.Items.Count == 0)
                {
                    _logger.LogWarning("実行対象コマンドがありません");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "実行対象がありません");
                    });
                    return;
                }

                // 開始処理（UI スレッドで実行）
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsRunning = true;
                    StatusChanged?.Invoke(this, "実行中...");
                });
                
                listPanelViewModel.SetRunningState(true);
                listPanelViewModel.InitializeProgress();
                
                _currentCancellationTokenSource = new CancellationTokenSource();
                var token = _currentCancellationTokenSource.Token;

                // MacroFactory にサービスを設定
                MacroFactory.SetServiceProvider(_serviceProvider);
                if (_pluginService != null)
                {
                    MacroFactory.SetPluginService(_pluginService);
                }

                // スナップショットを作成
                var itemsSnapshot = listPanelViewModel.Items.ToList();

                try
                {
                    // マクロ実行をバックグラウンドで開始
                    var root = MacroFactory.CreateMacro(itemsSnapshot);
                    var result = await root.Execute(token);

                    _logger.LogInformation("マクロ実行完了: {Result}", result);
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, result ? "実行完了" : "一部失敗/中断");
                    });
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("マクロがキャンセルされました");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "実行キャンセル");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "マクロ実行中にエラー");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, $"実行エラー: {ex.Message}");
                    });
                }
                finally
                {
                    // 終了処理（UI スレッドで実行）
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsRunning = false;
                    });
                    
                    listPanelViewModel.SetRunningState(false);
                    
                    _currentCancellationTokenSource?.Dispose();
                    _currentCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartMacroAsync 内でエラー");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusChanged?.Invoke(this, $"実行エラー: {ex.Message}");
                    IsRunning = false;
                });
                
                // ListPanelの状態もリセット
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                listPanelViewModel?.SetRunningState(false);
            }
        }

        private void SetupMessaging()
        {
            try
            {
                // メッセージング設定
                _messenger.Register<RunMessage>(this, (r, m) => { _ = StartMacroAsync(); });
                _messenger.Register<StopMessage>(this, (r, m) => { StopMacroInternal(); });
                
                _logger.LogDebug("ButtonService Messaging設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ButtonService Messaging設定中にエラー");
            }
        }
    }
}