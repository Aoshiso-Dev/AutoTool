using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Services;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Command.Interface;
using AutoTool.ViewModel.Panels;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Model.MacroFactory;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// ���C���E�B���h�E��ViewModel�iService�����Łj
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;
        private readonly IRecentFileService _recentFileService;
        private readonly IMessenger _messenger;
        private readonly IMainWindowMenuService _menuService;
        private readonly IMainWindowButtonService _buttonService;

        // ��{�v���p�e�B�iObservableProperty�ɕύX�j
        [ObservableProperty]
        private string _title = "AutoTool - �����}�N���������c�[��";
        
        [ObservableProperty]
        private double _windowWidth = 1200;
        
        [ObservableProperty]
        private double _windowHeight = 800;
        
        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private bool _isRunning = false;
        
        [ObservableProperty]
        private string _statusMessage = "��������";
        
        [ObservableProperty]
        private string _memoryUsage = "0 MB";
        
        [ObservableProperty]
        private string _cpuUsage = "0%";
        
        [ObservableProperty]
        private int _pluginCount = 0;
        
        [ObservableProperty]
        private int _commandCount = 0;
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFile = "�ۑ�(_S)";
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFileAs = "���O��t���ĕۑ�(_A)";

        // ���j���[�T�[�r�X����RecentFiles���擾
        public ObservableCollection<RecentFileItem> RecentFiles => _menuService?.RecentFiles ?? new();

        // ���j���[�R�}���h�iMenuService����擾�j
        public IRelayCommand OpenFileCommand => _menuService?.OpenFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand SaveFileCommand => _menuService?.SaveFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand SaveFileAsCommand => _menuService?.SaveFileAsCommand ?? new RelayCommand(() => { });
        public IRelayCommand ExitCommand => _menuService?.ExitCommand ?? new RelayCommand(() => { });
        public IRelayCommand ChangeThemeCommand => _menuService?.ChangeThemeCommand ?? new RelayCommand<string>(_ => { });
        public IRelayCommand LoadPluginFileCommand => _menuService?.LoadPluginFileCommand ?? new RelayCommand(() => { });
        public IRelayCommand RefreshPluginsCommand => _menuService?.RefreshPluginsCommand ?? new RelayCommand(() => { });
        public IRelayCommand ShowPluginInfoCommand => _menuService?.ShowPluginInfoCommand ?? new RelayCommand(() => { });
        public IRelayCommand OpenAppDirCommand => _menuService?.OpenAppDirCommand ?? new RelayCommand(() => { });
        public IRelayCommand RefreshPerformanceCommand => _menuService?.RefreshPerformanceCommand ?? new RelayCommand(() => { });
        public IRelayCommand ShowAboutCommand => _menuService?.ShowAboutCommand ?? new RelayCommand(() => { });
        public IRelayCommand ClearLogCommand => _menuService?.ClearLogCommand ?? new RelayCommand(() => { });

        // �{�^���R�}���h�iButtonService����擾�j
        public IRelayCommand RunMacroCommand => _buttonService?.RunMacroCommand ?? new RelayCommand(() => { });
        public IRelayCommand AddCommandCommand => _buttonService?.AddCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand DeleteCommandCommand => _buttonService?.DeleteCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand UpCommandCommand => _buttonService?.UpCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand DownCommandCommand => _buttonService?.DownCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand ClearCommandCommand => _buttonService?.ClearCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand UndoCommand => _buttonService?.UndoCommand ?? new RelayCommand(() => { });
        public IRelayCommand RedoCommand => _buttonService?.RedoCommand ?? new RelayCommand(() => { });
        public IRelayCommand AddTestCommandCommand => _buttonService?.AddTestCommandCommand ?? new RelayCommand(() => { });
        public IRelayCommand TestExecutionHighlightCommand => _buttonService?.TestExecutionHighlightCommand ?? new RelayCommand(() => { });

        // ����UI�֘A�v���p�e�B
        [ObservableProperty]
        private ICommandListItem? _selectedItem;
        
        [ObservableProperty]
        private int _selectedLineNumber = -1;
        
        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();
        
        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();
        
        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        // �v���O���X�֘A�v���p�e�B
        [ObservableProperty]
        private string _progressText = "";
        
        [ObservableProperty]
        private string _currentExecutingDescription = "";
        
        [ObservableProperty]
        private string _estimatedTimeRemaining = "";

        // �\������v���p�e�B�i�P�����j
        public bool IsListEmpty => CommandCount == 0;
        public bool IsListNotEmptyButNoSelection => CommandCount > 0 && SelectedItem == null;
        public bool IsNotNullItem => SelectedItem != null;

        /// <summary>
        /// �}�N�����s�\���ǂ���
        /// </summary>
        public bool CanRunMacro => _buttonService?.CanRunMacro ?? false;

        /// <summary>
        /// �}�N����~�\���ǂ���
        /// </summary>
        public bool CanStopMacro => _buttonService?.CanStopMacro ?? false;

        /// <summary>
        /// DI�Ή��R���X�g���N�^�iService�����Łj
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IRecentFileService recentFileService,
            IPluginService pluginService,
            IMainWindowMenuService menuService,
            IMainWindowButtonService buttonService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
            _messenger = WeakReferenceMessenger.Default;

            InitializeCommands();
            InitializeProperties();
            InitializeMessaging();
            LoadInitialData();
            SetupMenuServiceEvents();
            SetupButtonServiceEvents();

            _logger.LogInformation("MainWindowViewModel (Service������) �����������܂���");
        }

        /// <summary>
        /// �R�}���h�̏�����
        /// </summary>
        private void InitializeCommands()
        {
            try
            {
                // RelayCommand�͎������������̂ŁA�����ł͒ǉ��̏������̂�
                _logger.LogDebug("�R�}���h����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h���������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �v���p�e�B�̏�����
        /// </summary>
        private void InitializeProperties()
        {
            try
            {
                // �����l�ݒ�
                Title = "AutoTool - �����}�N���������c�[��";
                StatusMessage = "��������";
                WindowWidth = 1200;
                WindowHeight = 800;
                WindowState = WindowState.Normal;
                
                // �T���v�����O�ǉ�
                InitializeSampleLog();
                
                _logger.LogDebug("�v���p�e�B����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���p�e�B���������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// Messaging�ݒ�
        /// </summary>
        private void InitializeMessaging()
        {
            try
            {
                SetupMessaging();
                _logger.LogDebug("Messaging����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messaging���������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �ŋߊJ�����t�@�C���̓ǂݍ���
        /// </summary>
        private void LoadRecentFiles()
        {
            try
            {
                // IRecentFileService����ŋߊJ�����t�@�C�����擾
                var recentFiles = _recentFileService.GetRecentFiles();
                
                // MenuService��RecentFiles�ɒ��ڒǉ�
                _menuService.RecentFiles.Clear();
                foreach (var file in recentFiles.Take(10)) // �ő�10��
                {
                    _menuService.RecentFiles.Add(new RecentFileItem
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        LastAccessed = DateTime.Now
                    });
                }
                _logger.LogDebug("�ŋߊJ�����t�@�C���ǂݍ��݊���: {Count}��", _menuService.RecentFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߊJ�����t�@�C���ǂݍ��ݒ��ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���j���[�T�[�r�X�̃C�x���g�ݒ�
        /// </summary>
        private void SetupMenuServiceEvents()
        {
            try
            {
                if (_menuService != null)
                {
                    // �t�@�C���I�[�v���E�Z�[�u�C�x���g�̊Ď�
                    _menuService.FileOpened += (sender, filePath) =>
                    {
                        Title = $"AutoTool - {Path.GetFileName(filePath)}";
                        StatusMessage = $"�t�@�C�����J���܂���: {Path.GetFileName(filePath)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] �t�@�C���I�[�v��: {filePath}");
                    };

                    _menuService.FileSaved += (sender, filePath) =>
                    {
                        Title = $"AutoTool - {Path.GetFileName(filePath)}";
                        StatusMessage = $"�t�@�C����ۑ����܂���: {Path.GetFileName(filePath)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] �t�@�C���ۑ�: {filePath}");
                    };
                }

                _logger.LogDebug("MenuService �C�x���g�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MenuService �C�x���g�ݒ蒆�ɃG���[");
            }
        }

        /// <summary>
        /// �{�^���T�[�r�X�̃C�x���g�ݒ�
        /// </summary>
        private void SetupButtonServiceEvents()
        {
            try
            {
                if (_buttonService != null)
                {
                    // ���s��ԕύX�̊Ď�
                    _buttonService.RunningStateChanged += (sender, isRunning) =>
                    {
                        IsRunning = isRunning;
                        OnPropertyChanged(nameof(CanRunMacro));
                        OnPropertyChanged(nameof(CanStopMacro));
                        
                        // ListPanel�ɂ����s��Ԃ�ʒm
                        WeakReferenceMessenger.Default.Send(new MacroExecutionStateMessage(isRunning));
                        
                        _logger.LogDebug("�}�N�����s��ԕύX: {IsRunning}", isRunning);
                    };

                    // �X�e�[�^�X�ύX�̊Ď�
                    _buttonService.StatusChanged += (sender, status) =>
                    {
                        StatusMessage = status;
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {status}");
                    };

                    // �R�}���h���ύX�̊Ď�
                    _buttonService.CommandCountChanged += (sender, count) =>
                    {
                        CommandCount = count;
                        OnPropertyChanged(nameof(CanRunMacro));
                        OnPropertyChanged(nameof(CanStopMacro));
                    };
                }

                _logger.LogDebug("ButtonService �C�x���g�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ButtonService �C�x���g�ݒ蒆�ɃG���[");
            }
        }

        /// <summary>
        /// Messaging�ݒ�
        /// </summary>
        private void SetupMessaging()
        {
            try
            {
                // ListPanel����̏�ԕύX���b�Z�[�W����M
                _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
                {
                    SelectedItem = m.SelectedItem;
                    var listPanel = _serviceProvider.GetService<ListPanelViewModel>();
                    if (listPanel != null)
                    {
                        SelectedLineNumber = listPanel.SelectedIndex;
                        CommandCount = listPanel.TotalItems;
                    }
                    UpdateProperties();
                });

                // ListPanel����̃A�C�e�����ύX���b�Z�[�W����M
                _messenger.Register<ItemCountChangedMessage>(this, (r, m) =>
                {
                    CommandCount = m.Count;
                    _buttonService?.UpdateCommandCount(m.Count); // ButtonService�ɂ��ʒm
                    UpdateProperties();
                });

                // ListPanel����̃��O���b�Z�[�W����M
                _messenger.Register<LogMessage>(this, (r, m) =>
                {
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {m.Message}");
                });

                // ���j���[����̃��O�N���A�v������M
                _messenger.Register<ClearLogMessage>(this, (r, m) =>
                {
                    LogEntries.Clear();
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ���O�N���A");
                });

                _logger.LogDebug("Messaging�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Messaging�ݒ蒆�ɃG���[���������܂���");
            }
        }

        private void InitializeItemTypes()
        {
            try
            {
                // CommandRegistry���璼�ڎ擾
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                
                var commandTypes = AutoTool.Model.CommandDefinition.CommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                ItemTypes = new ObservableCollection<CommandDisplayItem>(commandTypes);
                SelectedItemType = ItemTypes.FirstOrDefault();
                _logger.LogDebug("ItemTypes����������: {Count}��", ItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes���������ɃG���[���������܂���");
                
                // �t�H�[���o�b�N
                ItemTypes = new ObservableCollection<CommandDisplayItem>
                {
                    new CommandDisplayItem { TypeName = "Wait", DisplayName = "�ҋ@", Category = "��{" }
                };
                SelectedItemType = ItemTypes.FirstOrDefault();
            }
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(IsListEmpty));
            OnPropertyChanged(nameof(IsListNotEmptyButNoSelection));
            OnPropertyChanged(nameof(IsNotNullItem));
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
        }

        private void InitializeSampleLog()
        {
            try
            {
                LogEntries.Add("[00:00:00] AutoTool Service����UI����������");
                LogEntries.Add("[00:00:01] �W��MVVM�����ɓ���");
                LogEntries.Add("[00:00:02] �T�[�r�X�����p�l���\������");
                _logger.LogDebug("�T���v�����O����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�T���v�����O���������ɃG���[���������܂���");
            }
        }

        partial void OnSelectedLineNumberChanged(int value)
        {
            UpdateProperties();
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            UpdateProperties();
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
            
            _logger.LogDebug("�}�N�����s��ԕύX: {IsRunning}", value);
        }

        partial void OnCommandCountChanged(int value)
        {
            UpdateProperties();
        }

        partial void OnSelectedItemTypeChanged(CommandDisplayItem? value)
        {
            // ButtonService�ɂ��I�����ꂽ�A�C�e���^�C�v��ʒm
            _buttonService?.SetSelectedItemType(value);
        }

        private void LoadInitialData()
        {
            try
            {
                // �R�}���h�^�C�v�̏�����
                InitializeItemTypes();
                
                // �ŋߊJ�����t�@�C����ǂݍ���
                LoadRecentFiles();

                _logger.LogInformation("�����f�[�^�̓ǂݍ��݂��������܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����f�[�^�̓ǂݍ��ݒ��ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �E�B���h�E�ݒ�̕ۑ�
        /// </summary>
        public void SaveWindowSettings()
        {
            try
            {
                _logger.LogDebug("�E�B���h�E�ݒ�ۑ��i�������j");
                // ��������\��
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E�ݒ�ۑ����ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �N���[���A�b�v����
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("�N���[���A�b�v�������s");
                // Messaging�̓o�^����
                _messenger.UnregisterAll(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�N���[���A�b�v�������ɃG���[���������܂���");
            }
        }
    }
}