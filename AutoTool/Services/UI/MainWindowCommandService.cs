using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Message;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// メインウィンドウのコマンド処理を管理するサービス
    /// </summary>
    public interface IMainWindowCommandService
    {
        // マクロ実行関連
        IRelayCommand RunMacroCommand { get; }
        IRelayCommand AddTestCommand { get; }
        IRelayCommand TestExecutionHighlightCommand { get; }
        
        // コマンド操作関連
        IRelayCommand AddCommand { get; }
        IRelayCommand DeleteCommand { get; }
        IRelayCommand UpCommand { get; }
        IRelayCommand DownCommand { get; }
        IRelayCommand ClearCommand { get; }
        IRelayCommand UndoCommand { get; }
        IRelayCommand RedoCommand { get; }
        
        // 状態プロパティ
        bool CanRunMacro { get; }
        bool CanStopMacro { get; }
        bool IsRunning { get; set; }
        int CommandCount { get; set; }
        
        // イベント
        event EventHandler<bool> RunningStateChanged;
    }

    /// <summary>
    /// メインウィンドウのコマンド処理サービス実装
    /// </summary>
    public class MainWindowCommandService : ObservableObject, IMainWindowCommandService
    {
        private readonly ILogger<MainWindowCommandService> _logger;
        private readonly IMessenger _messenger;
        private readonly Services.Execution.IMacroExecutionService _macroExecutionService;

        private bool _isRunning = false;
        private int _commandCount = 0;

        public bool IsRunning 
        { 
            get => _isRunning; 
            set 
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(CanRunMacro));
                    OnPropertyChanged(nameof(CanStopMacro));
                    RunMacroCommand.NotifyCanExecuteChanged();
                    RunningStateChanged?.Invoke(this, value);
                }
            }
        }
        
        public int CommandCount 
        { 
            get => _commandCount; 
            set 
            {
                if (SetProperty(ref _commandCount, value))
                {
                    OnPropertyChanged(nameof(CanRunMacro));
                    OnPropertyChanged(nameof(CanStopMacro));
                    RunMacroCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanRunMacro => !IsRunning && CommandCount > 0;
        public bool CanStopMacro => IsRunning;

        public event EventHandler<bool>? RunningStateChanged;

        // Command properties
        public IRelayCommand RunMacroCommand { get; }
        public IRelayCommand AddTestCommand { get; }
        public IRelayCommand TestExecutionHighlightCommand { get; }
        public IRelayCommand AddCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand UpCommand { get; }
        public IRelayCommand DownCommand { get; }
        public IRelayCommand ClearCommand { get; }
        public IRelayCommand UndoCommand { get; }
        public IRelayCommand RedoCommand { get; }

        public MainWindowCommandService(
            ILogger<MainWindowCommandService> logger,
            IMessenger messenger,
            Services.Execution.IMacroExecutionService macroExecutionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _macroExecutionService = macroExecutionService ?? throw new ArgumentNullException(nameof(macroExecutionService));

            // Initialize all commands
            RunMacroCommand = new RelayCommand(ExecuteRunMacro, CanExecuteRunMacro);
            AddTestCommand = new RelayCommand(ExecuteAddTestCommand);
            TestExecutionHighlightCommand = new RelayCommand(ExecuteTestExecutionHighlight);
            AddCommand = new RelayCommand(ExecuteAddCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            UpCommand = new RelayCommand(ExecuteUpCommand);
            DownCommand = new RelayCommand(ExecuteDownCommand);
            ClearCommand = new RelayCommand(ExecuteClearCommand);
            UndoCommand = new RelayCommand(ExecuteUndoCommand);
            RedoCommand = new RelayCommand(ExecuteRedoCommand);

            SetupMacroExecutionService();
        }

        private void SetupMacroExecutionService()
        {
            _macroExecutionService.RunningStateChanged += (s, isRunning) =>
            {
                IsRunning = isRunning;
            };
        }

        private bool CanExecuteRunMacro()
        {
            return IsRunning || (!IsRunning && CommandCount > 0);
        }

        private async void ExecuteRunMacro()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("マクロ停止要求");
                    await _macroExecutionService.StopAsync();
                }
                else
                {
                    _logger.LogInformation("マクロ実行要求");
                    // MacroExecutionServiceに実行を委譲
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroCommand実行中にエラー");
            }
        }

        private void ExecuteAddCommand()
        {
            try
            {
                _messenger.Send(new AddMessage("Wait")); // デフォルト
                _logger.LogDebug("Add command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCommand実行中にエラー");
            }
        }

        private void ExecuteDeleteCommand()
        {
            try
            {
                _messenger.Send(new DeleteMessage());
                _logger.LogDebug("Delete command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCommand実行中にエラー");
            }
        }

        private void ExecuteUpCommand()
        {
            try
            {
                _messenger.Send(new UpMessage());
                _logger.LogDebug("Up command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpCommand実行中にエラー");
            }
        }

        private void ExecuteDownCommand()
        {
            try
            {
                _messenger.Send(new DownMessage());
                _logger.LogDebug("Down command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownCommand実行中にエラー");
            }
        }

        private void ExecuteClearCommand()
        {
            try
            {
                _messenger.Send(new ClearMessage());
                _logger.LogDebug("Clear command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCommand実行中にエラー");
            }
        }

        private void ExecuteUndoCommand()
        {
            try
            {
                _messenger.Send(new UndoMessage());
                _logger.LogDebug("Undo command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UndoCommand実行中にエラー");
            }
        }

        private void ExecuteRedoCommand()
        {
            try
            {
                _messenger.Send(new RedoMessage());
                _logger.LogDebug("Redo command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedoCommand実行中にエラー");
            }
        }

        private void ExecuteAddTestCommand()
        {
            try
            {
                _messenger.Send(new AddMessage("Wait"));
                _logger.LogDebug("Test command added");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddTestCommand実行中にエラー");
            }
        }

        private void ExecuteTestExecutionHighlight()
        {
            try
            {
                // テスト用の実行ハイライト処理
                _logger.LogDebug("Execution highlight test started");
                // 詳細な実装は後で追加
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestExecutionHighlight実行中にエラー");
            }
        }
    }
}