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
    /// ���C���E�B���h�E�̃{�^���@�\���Ǘ�����T�[�r�X
    /// </summary>
    public interface IMainWindowButtonService
    {
        // ���s����
        IRelayCommand RunMacroCommand { get; }
        bool IsRunning { get; }
        bool CanRunMacro { get; }
        bool CanStopMacro { get; }
        
        // ���X�g����
        IRelayCommand AddCommandCommand { get; }
        IRelayCommand DeleteCommandCommand { get; }
        IRelayCommand UpCommandCommand { get; }
        IRelayCommand DownCommandCommand { get; }
        IRelayCommand ClearCommandCommand { get; }
        
        // ���𑀍�
        IRelayCommand UndoCommand { get; }
        IRelayCommand RedoCommand { get; }
        
        // �f�o�b�O�E�e�X�g
        IRelayCommand AddTestCommandCommand { get; }
        IRelayCommand TestExecutionHighlightCommand { get; }
        
        // �v���p�e�B
        CommandDisplayItem? SelectedItemType { get; set; }
        int CommandCount { get; }
        
        // �C�x���g
        event EventHandler<bool> RunningStateChanged;
        event EventHandler<string> StatusChanged;
        event EventHandler<int> CommandCountChanged;
        
        // ���\�b�h
        void UpdateCommandCount(int count);
        void SetSelectedItemType(CommandDisplayItem? itemType);
    }

    /// <summary>
    /// ���C���E�B���h�E�̃{�^���@�\�T�[�r�X����
    /// </summary>
    public partial class MainWindowButtonService : IMainWindowButtonService
    {
        private readonly ILogger<MainWindowButtonService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;
        private readonly IMessenger _messenger;
        
        // �}�N�����s�֘A
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
                    
                    // CanExecute��Ԃ��X�V
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
                    
                    // ���s�\��Ԃ��X�V
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
                    _logger.LogInformation("��~�v���𑗐M���܂�");
                    StatusChanged?.Invoke(this, "��~�v���𑗐M���܂���");
                    
                    // ��~������ʃ^�X�N�Ŏ��s�iUI���u���b�N���Ȃ��j
                    _ = Task.Run(() => StopMacroInternal());
                }
                else
                {
                    _logger.LogInformation("���s�v�����J�n���܂�");
                    StatusChanged?.Invoke(this, "���s������...");
                    
                    // �񓯊��Ń}�N�����s���J�n�iUI�X���b�h���u���b�N���Ȃ��j
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await StartMacroAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "�o�b�N�O���E���h�}�N�����s���ɃG���[");
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                StatusChanged?.Invoke(this, $"���s�G���[: {ex.Message}");
                                IsRunning = false;
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacroCommand ���s���ɃG���[");
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusChanged?.Invoke(this, $"���s�G���[: {ex.Message}");
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
                    _logger.LogDebug("�ǉ��v��: {Type}", SelectedItemType.TypeName);
                    _messenger.Send(new AddMessage(SelectedItemType.TypeName));
                }
                else
                {
                    _logger.LogWarning("�ǉ��v��: SelectedItemType �� null �ł�");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCommand ���s���ɃG���[");
            }
        }

        private void ExecuteDeleteCommand()
        {
            try
            {
                _logger.LogDebug("�폜�v���𑗐M");
                _messenger.Send(new DeleteMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCommand ���s���ɃG���[");
            }
        }

        private void ExecuteUpCommand()
        {
            try
            {
                _logger.LogDebug("��ړ��v���𑗐M");
                _messenger.Send(new UpMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpCommand ���s���ɃG���[");
            }
        }

        private void ExecuteDownCommand()
        {
            try
            {
                _logger.LogDebug("���ړ��v���𑗐M");
                _messenger.Send(new DownMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownCommand ���s���ɃG���[");
            }
        }

        private void ExecuteClearCommand()
        {
            try
            {
                _logger.LogDebug("�N���A�v���𑗐M");
                _messenger.Send(new ClearMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCommand ���s���ɃG���[");
            }
        }

        private void ExecuteUndoCommand()
        {
            try
            {
                _logger.LogDebug("Undo�v���𑗐M");
                _messenger.Send(new UndoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UndoCommand ���s���ɃG���[");
            }
        }

        private void ExecuteRedoCommand()
        {
            try
            {
                _logger.LogDebug("Redo�v���𑗐M");
                _messenger.Send(new RedoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedoCommand ���s���ɃG���[");
            }
        }

        private void ExecuteAddTestCommand()
        {
            try
            {
                _logger.LogDebug("�e�X�g�R�}���h�ǉ��v�����M�i���IUI�Łj");
                
                // ���IUI��UniversalCommandItem���쐬
                var testItem = AutoTool.Model.CommandDefinition.DirectCommandRegistry.CreateUniversalItem("Test");
                
                // AddUniversalItemMessage�𑗐M
                _messenger.Send(new AddUniversalItemMessage(testItem));
                
                StatusChanged?.Invoke(this, "���I�e�X�g�R�}���h��ǉ����܂���");
                _logger.LogInformation("���ITestCommand������ɒǉ�����܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���I�e�X�g�R�}���h�ǉ��ŃG���[���������܂���");
                StatusChanged?.Invoke(this, $"���I�e�X�g�R�}���h�ǉ��G���[: {ex.Message}");
            }
        }

        private void ExecuteTestExecutionHighlight()
        {
            try
            {
                _logger.LogDebug("���s�n�C���C�g�e�X�g�J�n");
                
                // DI����ListPanelViewModel���擾
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                if (listPanelViewModel == null)
                {
                    StatusChanged?.Invoke(this, "ListPanelViewModel��������܂���");
                    return;
                }

                if (listPanelViewModel.Items.Count == 0)
                {
                    StatusChanged?.Invoke(this, "�e�X�g�Ώۂ̃R�}���h������܂���");
                    return;
                }

                StatusChanged?.Invoke(this, "���s�n�C���C�g�e�X�g�J�n");

                // �ŏ��̃A�C�e�������s����Ԃɂ���
                var firstItem = listPanelViewModel.Items.First();
                
                // UI�X���b�h�Ŏ��s��Ԃ�ݒ�
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    firstItem.IsRunning = true;
                    firstItem.Progress = 0;
                    listPanelViewModel.CurrentExecutingItem = firstItem;
                });

                // �v���O���X��i�K�I�ɍX�V
                Task.Run(async () =>
                {
                    for (int i = 0; i <= 100; i += 10)
                    {
                        await Task.Delay(500); // 500ms���ƂɍX�V
                        
                        // UI�X���b�h�Ŏ��s
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            firstItem.Progress = i;
                        });
                    }

                    // ������Ԃɐݒ�
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        firstItem.IsRunning = false;
                        firstItem.Progress = 100;
                        listPanelViewModel.CurrentExecutingItem = null;
                        StatusChanged?.Invoke(this, "���s�n�C���C�g�e�X�g����");

                        // �����҂��Ă���v���O���X�����Z�b�g
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
                _logger.LogError(ex, "���s�n�C���C�g�e�X�g���ɃG���[���������܂���");
                StatusChanged?.Invoke(this, $"���s�n�C���C�g�e�X�g�G���[: {ex.Message}");
            }
        }

        private void StopMacroInternal()
        {
            try
            {
                _logger.LogInformation("��~�v������M���܂���");
                
                if (_currentCancellationTokenSource != null && !_currentCancellationTokenSource.IsCancellationRequested)
                {
                    _logger.LogInformation("�L�����Z���v���𑗐M���܂���");
                    _currentCancellationTokenSource.Cancel();
                    
                    // UI��Ԃ𑦍��ɍX�V
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusChanged?.Invoke(this, "��~������...");
                    });
                }
                else
                {
                    _logger.LogWarning("�L�����Z���v��: ���ɃL�����Z���ς݂��g�[�N���\�[�X��null");
                    
                    // ��Ԃ��s���ȏꍇ�͋������Z�b�g
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (IsRunning)
                        {
                            IsRunning = false;
                            StatusChanged?.Invoke(this, "������~����");
                            
                            // ListPanel�̏�Ԃ����Z�b�g
                            var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                            listPanelViewModel?.SetRunningState(false);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��~�������ɃG���[");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusChanged?.Invoke(this, $"��~�G���[: {ex.Message}");
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
                    _logger.LogError("ListPanelViewModel ��������܂���B���s�𒆎~���܂��B");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "���s�G���[: ListPanel VM �擾���s");
                    });
                    return;
                }

                if (IsRunning)
                {
                    _logger.LogWarning("���Ɏ��s���̂��ߊJ�n���܂���");
                    return;
                }
                
                if (listPanelViewModel.Items.Count == 0)
                {
                    _logger.LogWarning("���s�ΏۃR�}���h������܂���");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "���s�Ώۂ�����܂���");
                    });
                    return;
                }

                // �J�n�����iUI �X���b�h�Ŏ��s�j
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsRunning = true;
                    StatusChanged?.Invoke(this, "���s��...");
                });
                
                listPanelViewModel.SetRunningState(true);
                listPanelViewModel.InitializeProgress();
                
                _currentCancellationTokenSource = new CancellationTokenSource();
                var token = _currentCancellationTokenSource.Token;

                // MacroFactory �ɃT�[�r�X��ݒ�
                MacroFactory.SetServiceProvider(_serviceProvider);
                if (_pluginService != null)
                {
                    MacroFactory.SetPluginService(_pluginService);
                }

                // �X�i�b�v�V���b�g���쐬
                var itemsSnapshot = listPanelViewModel.Items.ToList();

                try
                {
                    // �}�N�����s���o�b�N�O���E���h�ŊJ�n
                    var root = MacroFactory.CreateMacro(itemsSnapshot);
                    var result = await root.Execute(token);

                    _logger.LogInformation("�}�N�����s����: {Result}", result);
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, result ? "���s����" : "�ꕔ���s/���f");
                    });
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("�}�N�����L�����Z������܂���");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, "���s�L�����Z��");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "�}�N�����s���ɃG���[");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusChanged?.Invoke(this, $"���s�G���[: {ex.Message}");
                    });
                }
                finally
                {
                    // �I�������iUI �X���b�h�Ŏ��s�j
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
                _logger.LogError(ex, "StartMacroAsync ���ŃG���[");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusChanged?.Invoke(this, $"���s�G���[: {ex.Message}");
                    IsRunning = false;
                });
                
                // ListPanel�̏�Ԃ����Z�b�g
                var listPanelViewModel = _serviceProvider.GetService<ListPanelViewModel>();
                listPanelViewModel?.SetRunningState(false);
            }
        }

        private void SetupMessaging()
        {
            try
            {
                // ���b�Z�[�W���O�ݒ�
                _messenger.Register<RunMessage>(this, (r, m) => { _ = StartMacroAsync(); });
                _messenger.Register<StopMessage>(this, (r, m) => { StopMacroInternal(); });
                
                _logger.LogDebug("ButtonService Messaging�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ButtonService Messaging�ݒ蒆�ɃG���[");
            }
        }
    }
}