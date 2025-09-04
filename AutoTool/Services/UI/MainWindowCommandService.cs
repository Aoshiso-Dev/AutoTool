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
    /// ���C���E�B���h�E�̃R�}���h�������Ǘ�����T�[�r�X
    /// </summary>
    public interface IMainWindowCommandService
    {
        // �}�N�����s�֘A
        IRelayCommand RunMacroCommand { get; }
        IRelayCommand AddTestCommand { get; }
        IRelayCommand TestExecutionHighlightCommand { get; }
        
        // �R�}���h����֘A
        IRelayCommand AddCommand { get; }
        IRelayCommand DeleteCommand { get; }
        IRelayCommand UpCommand { get; }
        IRelayCommand DownCommand { get; }
        IRelayCommand ClearCommand { get; }
        IRelayCommand UndoCommand { get; }
        IRelayCommand RedoCommand { get; }
        
        // ��ԃv���p�e�B
        bool CanRunMacro { get; }
        bool CanStopMacro { get; }
        bool IsRunning { get; set; }
        int CommandCount { get; set; }
        
        // �C�x���g
        event EventHandler<bool> RunningStateChanged;
    }

    /// <summary>
    /// ���C���E�B���h�E�̃R�}���h�����T�[�r�X����
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
                    _logger.LogInformation("�}�N����~�v��");
                    await _macroExecutionService.StopAsync();
                }
                else
                {
                    _logger.LogInformation("�}�N�����s�v��");
                    // MacroExecutionService�Ɏ��s���Ϗ�
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroCommand���s���ɃG���[");
            }
        }

        private void ExecuteAddCommand()
        {
            try
            {
                _messenger.Send(new AddMessage("Wait")); // �f�t�H���g
                _logger.LogDebug("Add command sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCommand���s���ɃG���[");
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
                _logger.LogError(ex, "DeleteCommand���s���ɃG���[");
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
                _logger.LogError(ex, "UpCommand���s���ɃG���[");
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
                _logger.LogError(ex, "DownCommand���s���ɃG���[");
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
                _logger.LogError(ex, "ClearCommand���s���ɃG���[");
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
                _logger.LogError(ex, "UndoCommand���s���ɃG���[");
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
                _logger.LogError(ex, "RedoCommand���s���ɃG���[");
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
                _logger.LogError(ex, "AddTestCommand���s���ɃG���[");
            }
        }

        private void ExecuteTestExecutionHighlight()
        {
            try
            {
                // �e�X�g�p�̎��s�n�C���C�g����
                _logger.LogDebug("Execution highlight test started");
                // �ڍׂȎ����͌�Œǉ�
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestExecutionHighlight���s���ɃG���[");
            }
        }
    }
}