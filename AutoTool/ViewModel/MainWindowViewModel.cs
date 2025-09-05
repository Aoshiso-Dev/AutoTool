using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Services;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.ViewModel.Panels;
using AutoTool.ViewModel.Shared;
using AutoTool.Model.CommandDefinition;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// MainWindowViewModel (DirectCommandRegistry������)
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRecentFileService _recentFileService;
        private readonly IPluginService _pluginService;
        private readonly IMainWindowMenuService _menuService;
        private readonly IMainWindowButtonService _buttonService;

        [ObservableProperty]
        private string _title = "AutoTool - �������c�[��";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "��������";

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _availableCommands = new();

        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private bool _isMaximized = false;

        // ViewModel�v���p�e�B
        public ListPanelViewModel ListPanelViewModel { get; }
        public EditPanelViewModel EditPanelViewModel { get; }
        public ButtonPanelViewModel ButtonPanelViewModel { get; }

        // ���v�v���p�e�B
        public int CommandCount => ListPanelViewModel.Items.Count;
        public bool HasCommands => CommandCount > 0;

        // ���̑��̃_�~�[�R�}���h�i�o�C���f�B���O�G���[���p�j
        [RelayCommand]
        private void ChangeTheme(string theme)
        {
            try
            {
                _logger.LogDebug("�e�[�}�ύX�v��: {Theme}", theme);
                StatusMessage = $"�e�[�}��{theme}�ɕύX���܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�ύX���ɃG���[");
                StatusMessage = $"�e�[�}�ύX�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void RefreshPerformance()
        {
            StatusMessage = "�p�t�H�[�}���X�����X�V���܂���";
        }

        [RelayCommand]
        private void LoadPluginFile()
        {
            StatusMessage = "�v���O�C���ǂݍ��݋@�\�͖������ł�";
        }

        [RelayCommand]
        private void RefreshPlugins()
        {
            StatusMessage = "�v���O�C���ēǂݍ��݋@�\�͖������ł�";
        }

        [RelayCommand]
        private void ShowPluginInfo()
        {
            StatusMessage = "�v���O�C�����\���@�\�͖������ł�";
        }

        [RelayCommand]
        private void OpenAppDir()
        {
            StatusMessage = "�A�v���P�[�V�����t�H���_���J���@�\�͖������ł�";
        }

        [RelayCommand]
        private void ClearLog()
        {
            StatusMessage = "���O���N���A���܂���";
        }

        // �v���p�e�B�i�o�C���f�B���O�G���[���p�j
        public string MenuItemHeader_SaveFile => "�ۑ�(_S)";
        public string MenuItemHeader_SaveFileAs => "���O��t���ĕۑ�(_A)";
        public ObservableCollection<object> RecentFiles { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        public string MemoryUsage => "0 MB";
        public string CpuUsage => "0%";
        public int PluginCount => 0;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IRecentFileService recentFileService,
            IPluginService pluginService,
            IMainWindowMenuService menuService,
            IMainWindowButtonService buttonService,
            ListPanelViewModel listPanelViewModel,
            EditPanelViewModel editPanelViewModel,
            ButtonPanelViewModel buttonPanelViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
            
            ListPanelViewModel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
            EditPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            ButtonPanelViewModel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));

            _logger.LogInformation("MainWindowViewModel (DirectCommandRegistry��) �������J�n");

            // ListPanelViewModel�̃A�C�e�����ύX���Ď�
            ListPanelViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListPanelViewModel.Items))
                {
                    OnPropertyChanged(nameof(CommandCount));
                    OnPropertyChanged(nameof(HasCommands));
                }
            };

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                LoadAvailableCommands();
                StatusMessage = "����������";
                _logger.LogInformation("MainWindowViewModel ����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindowViewModel ���������ɃG���[");
                StatusMessage = $"�������G���[: {ex.Message}";
            }
        }

        private void LoadAvailableCommands()
        {
            try
            {
                _logger.LogDebug("���p�\�ȃR�}���h�̓ǂݍ��݊J�n");
                
                var displayItems = DirectCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = DirectCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                AvailableCommands = new ObservableCollection<CommandDisplayItem>(displayItems);
                
                _logger.LogDebug("���p�\�ȃR�}���h�ǂݍ��݊���: {Count}��", AvailableCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���p�\�ȃR�}���h�ǂݍ��ݒ��ɃG���[");
                AvailableCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        [RelayCommand]
        private async Task LoadFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "�t�@�C����ǂݍ��ݒ�...";
                
                // TODO: ���j���[�T�[�r�X��LoadFileAsync���\�b�h��ǉ�����K�v������܂�
                // await _menuService.LoadFileAsync();
                await Task.Delay(100); // �ꎞ�I�ȑ��
                
                StatusMessage = "�t�@�C���ǂݍ��݊���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[");
                StatusMessage = $"�ǂݍ��݃G���[: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "�t�@�C����ۑ���...";
                
                // TODO: ���j���[�T�[�r�X��SaveFileAsync���\�b�h��ǉ�����K�v������܂�
                // await _menuService.SaveFileAsync();
                await Task.Delay(100); // �ꎞ�I�ȑ��
                
                StatusMessage = "�t�@�C���ۑ�����";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[");
                StatusMessage = $"�ۑ��G���[: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            try
            {
                _logger.LogInformation("�A�v���P�[�V�����I���v��");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����I�����ɃG���[");
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            try
            {
                _logger.LogDebug("About �_�C�A���O�\��");
                StatusMessage = "AutoTool v1.0 - �������c�[��";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "About �_�C�A���O�\�����ɃG���[");
            }
        }

        public void SetStatus(string message)
        {
            StatusMessage = message;
            _logger.LogDebug("�X�e�[�^�X�X�V: {Message}", message);
        }

        public void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
            if (isLoading)
            {
                StatusMessage = "������...";
            }
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            _logger.LogTrace("�v���p�e�B�ύX: {PropertyName}", e.PropertyName);
        }
    }
}