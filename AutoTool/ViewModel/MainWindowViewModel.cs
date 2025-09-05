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
using System.Collections.ObjectModel;
using System.Linq;
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
    /// ���C���E�B���h�E��ViewModel
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMainWindowButtonService _buttonService;
        private readonly IMainWindowMenuService _menuService;
        private readonly IRecentFileService _recentFileService;

        // Window properties
        [ObservableProperty]
        private string _title = "AutoTool";

        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;

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
        private bool _isLoading = false;

        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();

        [ObservableProperty]
        private ObservableCollection<RecentFileItem> _recentFiles = new();

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        // Button service properties (�Ϗ�)
        public bool IsRunning => _buttonService?.IsRunning ?? false;
        public CommandDisplayItem? SelectedItemType 
        { 
            get => _buttonService.SelectedItemType; 
            set => _buttonService.SetSelectedItemType(value); 
        }

        // Commands (�Ϗ�)
        public IRelayCommand RunMacroCommand => _buttonService.RunMacroCommand;
        public IRelayCommand AddCommandCommand => _buttonService.AddCommandCommand;
        public IRelayCommand DeleteCommandCommand => _buttonService.DeleteCommandCommand;
        public IRelayCommand UpCommandCommand => _buttonService.UpCommandCommand;
        public IRelayCommand DownCommandCommand => _buttonService.DownCommandCommand;
        public IRelayCommand ClearCommandCommand => _buttonService.ClearCommandCommand;
        public IRelayCommand UndoCommand => _buttonService.UndoCommand;
        public IRelayCommand RedoCommand => _buttonService.RedoCommand;
        public IRelayCommand AddTestCommandCommand => _buttonService.AddTestCommandCommand;
        public IRelayCommand TestExecutionHighlightCommand => _buttonService.TestExecutionHighlightCommand;

        // Menu service commands (�Ϗ�)
        public IRelayCommand OpenFileCommand => _menuService.OpenFileCommand;
        public IRelayCommand SaveFileCommand => _menuService.SaveFileCommand;
        public IRelayCommand SaveFileAsCommand => _menuService.SaveFileAsCommand;
        public IRelayCommand ExitCommand => _menuService.ExitCommand;
        public IRelayCommand ShowAboutCommand => _menuService.ShowAboutCommand;
        public IRelayCommand ClearLogCommand { get; }

        // Menu headers
        public string MenuItemHeader_SaveFile => "�ۑ�(_S)";
        public string MenuItemHeader_SaveFileAs => "���O��t���ĕۑ�(_A)";

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IMainWindowButtonService buttonService,
            IMainWindowMenuService menuService,
            IRecentFileService recentFileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));

            // Initialize commands
            ClearLogCommand = new RelayCommand(ClearLog);

            // Setup event handlers
            SetupEventHandlers();

            // Initialize collections
            InitializeCollections();

            _logger.LogInformation("MainWindowViewModel����������");
        }

        private void InitializeCollections()
        {
            try
            {
                // CommandTypes��������
                CommandRegistry.Initialize();
                var displayItems = CommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                
                _logger.LogInformation("�R�}���h�^�C�v�����������܂���: {Count}��", ItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�^�C�v�������G���[");
                ItemTypes = new ObservableCollection<CommandDisplayItem>();
            }
        }

        private void SetupEventHandlers()
        {
            // Button service events
            _buttonService.RunningStateChanged += OnRunningStateChanged;
            _buttonService.StatusChanged += OnStatusChanged;
            _buttonService.CommandCountChanged += OnCommandCountChanged;
        }

        private void OnRunningStateChanged(object? sender, bool isRunning)
        {
            OnPropertyChanged(nameof(IsRunning));
            StatusMessage = isRunning ? "���s��..." : "��������";
        }

        private void OnStatusChanged(object? sender, string message)
        {
            StatusMessage = message;
            LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // ���O�������Ȃ肷�����ꍇ�͌Â����̂��폜
            if (LogEntries.Count > 1000)
            {
                LogEntries.RemoveAt(0);
            }
        }

        private void OnCommandCountChanged(object? sender, int count)
        {
            CommandCount = count;
        }

        private void ClearLog()
        {
            LogEntries.Clear();
            _logger.LogInformation("���O���N���A���܂���");
        }
    }

    /// <summary>
    /// �ŋߎg�p�����t�@�C���A�C�e��
    /// </summary>
    public class RecentFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}