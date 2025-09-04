using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using AutoTool.Services.UI;
using AutoTool.Services.Configuration;
using AutoTool.Message;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// ���C���E�B���h�E�̃��j���[�@�\���Ǘ�����T�[�r�X
    /// </summary>
    public interface IMainWindowMenuService
    {
        // �t�@�C������
        IRelayCommand OpenFileCommand { get; }
        IRelayCommand SaveFileCommand { get; }
        IRelayCommand SaveFileAsCommand { get; }
        IRelayCommand ExitCommand { get; }
        IRelayCommand OpenRecentFileCommand { get; }
        
        // �e�[�}����
        IRelayCommand ChangeThemeCommand { get; }
        
        // �v���O�C������
        IRelayCommand LoadPluginFileCommand { get; }
        IRelayCommand RefreshPluginsCommand { get; }
        IRelayCommand ShowPluginInfoCommand { get; }
        
        // �c�[������
        IRelayCommand OpenAppDirCommand { get; }
        IRelayCommand RefreshPerformanceCommand { get; }
        
        // �w���v����
        IRelayCommand ShowAboutCommand { get; }
        
        // ���O����
        IRelayCommand ClearLogCommand { get; }
        
        // �v���p�e�B
        string CurrentFilePath { get; set; }
        ObservableCollection<RecentFileItem> RecentFiles { get; }
        
        // �C�x���g
        event EventHandler<string> FileOpened;
        event EventHandler<string> FileSaved;
    }

    /// <summary>
    /// �ŋߊJ�����t�@�C���̃A�C�e��
    /// </summary>
    public partial class RecentFileItem : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = string.Empty;
        
        [ObservableProperty]
        private string _filePath = string.Empty;
        
        [ObservableProperty]
        private DateTime _lastAccessed = DateTime.Now;
    }

    /// <summary>
    /// ���C���E�B���h�E�̃��j���[�@�\�T�[�r�X����
    /// </summary>
    public partial class MainWindowMenuService : ObservableObject, IMainWindowMenuService
    {
        private readonly ILogger<MainWindowMenuService> _logger;
        private readonly IMessenger _messenger;
        private readonly IEnhancedThemeService _themeService;
        private readonly IEnhancedConfigurationService _configService;
        
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

        // Command properties
        public IRelayCommand OpenFileCommand { get; }
        public IRelayCommand SaveFileCommand { get; }
        public IRelayCommand SaveFileAsCommand { get; }
        public IRelayCommand ExitCommand { get; }
        public IRelayCommand OpenRecentFileCommand { get; }
        public IRelayCommand ChangeThemeCommand { get; }
        public IRelayCommand LoadPluginFileCommand { get; }
        public IRelayCommand RefreshPluginsCommand { get; }
        public IRelayCommand ShowPluginInfoCommand { get; }
        public IRelayCommand OpenAppDirCommand { get; }
        public IRelayCommand RefreshPerformanceCommand { get; }
        public IRelayCommand ShowAboutCommand { get; }
        public IRelayCommand ClearLogCommand { get; }

        public event EventHandler<string>? FileOpened;
        public event EventHandler<string>? FileSaved;

        public MainWindowMenuService(
            ILogger<MainWindowMenuService> logger,
            IMessenger messenger,
            IEnhancedThemeService themeService,
            IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            // Initialize commands
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            SaveFileCommand = new RelayCommand(ExecuteSaveFile);
            SaveFileAsCommand = new RelayCommand(ExecuteSaveFileAs);
            ExitCommand = new RelayCommand(ExecuteExit);
            OpenRecentFileCommand = new RelayCommand<string>(ExecuteOpenRecentFile);
            ChangeThemeCommand = new RelayCommand<string>(ExecuteChangeTheme);
            LoadPluginFileCommand = new RelayCommand(ExecuteLoadPluginFile);
            RefreshPluginsCommand = new RelayCommand(ExecuteRefreshPlugins);
            ShowPluginInfoCommand = new RelayCommand(ExecuteShowPluginInfo);
            OpenAppDirCommand = new RelayCommand(ExecuteOpenAppDir);
            RefreshPerformanceCommand = new RelayCommand(ExecuteRefreshPerformance);
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);

            LoadRecentFiles();
        }

        private void ExecuteOpenFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "�}�N���t�@�C�����J��",
                    Filter = "AutoTool �}�N���t�@�C�� (*.atm)|*.atm|JSON�t�@�C�� (*.json)|*.json|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".atm",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    _messenger.Send(new LoadMessage(filePath));
                    
                    CurrentFilePath = filePath;
                    AddToRecentFiles(filePath);
                    FileOpened?.Invoke(this, filePath);
                    
                    _logger.LogInformation("�t�@�C�����J���܂���: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���I�[�v�����ɃG���[������");
                System.Windows.MessageBox.Show($"�t�@�C�����J���ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveFile()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilePath))
                {
                    ExecuteSaveFileAs();
                    return;
                }

                _messenger.Send(new SaveMessage(CurrentFilePath));
                FileSaved?.Invoke(this, CurrentFilePath);
                
                _logger.LogInformation("�t�@�C����ۑ����܂���: {FilePath}", CurrentFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[������");
                System.Windows.MessageBox.Show($"�t�@�C����ۑ�����ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveFileAs()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "�}�N���t�@�C���𖼑O��t���ĕۑ�",
                    Filter = "AutoTool �}�N���t�@�C�� (*.atm)|*.atm|JSON�t�@�C�� (*.json)|*.json|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".atm",
                    FileName = Path.GetFileNameWithoutExtension(CurrentFilePath) + ".atm"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var filePath = saveFileDialog.FileName;
                    _messenger.Send(new SaveMessage(filePath));
                    
                    CurrentFilePath = filePath;
                    AddToRecentFiles(filePath);
                    FileSaved?.Invoke(this, filePath);
                    
                    _logger.LogInformation("�t�@�C���𖼑O��t���ĕۑ����܂���: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O��t���ĕۑ����ɃG���[������");
                System.Windows.MessageBox.Show($"�t�@�C����ۑ�����ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteExit()
        {
            try
            {
                _logger.LogInformation("�A�v���P�[�V�����I���v��");
                System.Windows.Application.Current?.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����I�����ɃG���[������");
            }
        }

        private void ExecuteOpenRecentFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (File.Exists(filePath))
                {
                    _messenger.Send(new LoadMessage(filePath));
                    CurrentFilePath = filePath;
                    UpdateRecentFileAccess(filePath);
                    FileOpened?.Invoke(this, filePath);
                    
                    _logger.LogInformation("�ŋ߂̃t�@�C�����J���܂���: {FilePath}", filePath);
                }
                else
                {
                    System.Windows.MessageBox.Show($"�t�@�C����������܂���B\n{filePath}",
                        "�t�@�C����������܂���", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    RemoveFromRecentFiles(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋ߂̃t�@�C���I�[�v�����ɃG���[������: {FilePath}", filePath);
                System.Windows.MessageBox.Show($"�t�@�C�����J���ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteChangeTheme(string? themeName)
        {
            if (string.IsNullOrEmpty(themeName)) return;

            try
            {
                var theme = ThemeDefinitionFactory.ParseTheme(themeName);
                _themeService.SetTheme(theme);
                
                _logger.LogInformation("�e�[�}��ύX���܂���: {Theme}", themeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�ύX���ɃG���[������: {Theme}", themeName);
                System.Windows.MessageBox.Show($"�e�[�}��ύX����ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteLoadPluginFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "�v���O�C���t�@�C����I��",
                    Filter = "�v���O�C���t�@�C�� (*.dll)|*.dll|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".dll",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        // �v���O�C���ǂݍ��ݏ��������b�Z�[�W�Œʒm
                        _messenger.Send(new LoadPluginMessage(filePath));
                    }
                    
                    _logger.LogInformation("�v���O�C���t�@�C���̓ǂݍ��ݗv��: {Count}��", openFileDialog.FileNames.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���t�@�C���ǂݍ��ݒ��ɃG���[������");
                System.Windows.MessageBox.Show($"�v���O�C����ǂݍ��ލۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefreshPlugins()
        {
            try
            {
                _messenger.Send(new RefreshPluginsMessage());
                _logger.LogInformation("�v���O�C���ēǂݍ��ݗv��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���ēǂݍ��ݒ��ɃG���[������");
            }
        }

        private void ExecuteShowPluginInfo()
        {
            try
            {
                _messenger.Send(new ShowPluginInfoMessage());
                _logger.LogInformation("�v���O�C�����\���v��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C�����\�����ɃG���[������");
            }
        }

        private void ExecuteOpenAppDir()
        {
            try
            {
                var appDir = AppContext.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = appDir,
                    UseShellExecute = true
                });
                
                _logger.LogInformation("�A�v���P�[�V�����t�H���_���J���܂���: {AppDir}", appDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����t�H���_�I�[�v�����ɃG���[������");
                System.Windows.MessageBox.Show($"�t�H���_���J���ۂɃG���[���������܂����B\n\n{ex.Message}",
                    "�G���[", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefreshPerformance()
        {
            try
            {
                _messenger.Send(new RefreshPerformanceMessage());
                _logger.LogInformation("�p�t�H�[�}���X���X�V�v��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�p�t�H�[�}���X���X�V���ɃG���[������");
            }
        }

        private void ExecuteShowAbout()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "�s��";
                var message = $"AutoTool - �����}�N���������c�[��\n\n�o�[�W����: {version}\n\n" +
                             "? 2024 AutoTool Development Team\n\n" +
                             "���̃\�t�g�E�F�A�́A�J��Ԃ���Ƃ̎��������x������c�[���ł��B";
                
                System.Windows.MessageBox.Show(message, "AutoTool �ɂ���", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                _logger.LogInformation("�o�[�W��������\�����܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�o�[�W�������\�����ɃG���[������");
            }
        }

        private void ExecuteClearLog()
        {
            try
            {
                _messenger.Send(new ClearLogMessage());
                _logger.LogInformation("���O�N���A�v��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�N���A���ɃG���[������");
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var existingItem = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
                
                if (existingItem != null)
                {
                    existingItem.LastAccessed = DateTime.Now;
                    // �����A�C�e����擪�Ɉړ�
                    RecentFiles.Remove(existingItem);
                    RecentFiles.Insert(0, existingItem);
                }
                else
                {
                    // �V�����A�C�e����擪�ɒǉ�
                    RecentFiles.Insert(0, new RecentFileItem
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        LastAccessed = DateTime.Now
                    });
                }

                // �ő�10���܂ŕێ�
                while (RecentFiles.Count > 10)
                {
                    RecentFiles.RemoveAt(RecentFiles.Count - 1);
                }

                SaveRecentFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋ߂̃t�@�C�����X�g�X�V���ɃG���[������");
            }
        }

        private void UpdateRecentFileAccess(string filePath)
        {
            var item = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (item != null)
            {
                item.LastAccessed = DateTime.Now;
                RecentFiles.Remove(item);
                RecentFiles.Insert(0, item);
                SaveRecentFiles();
            }
        }

        private void RemoveFromRecentFiles(string filePath)
        {
            var item = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (item != null)
            {
                RecentFiles.Remove(item);
                SaveRecentFiles();
            }
        }

        private void LoadRecentFiles()
        {
            try
            {
                var recentFilesData = _configService.GetValue("UI:RecentFiles", new List<object>());
                // �ŋ߂̃t�@�C���̕�������������
                _logger.LogDebug("�ŋ߂̃t�@�C����ǂݍ��݂܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋ߂̃t�@�C���ǂݍ��ݒ��ɃG���[������");
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                var recentFilesData = RecentFiles.Take(10).Select(f => new
                {
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    LastAccessed = f.LastAccessed
                }).ToList();
                
                _configService.SetValue("UI:RecentFiles", recentFilesData);
                _configService.Save();
                
                _logger.LogDebug("�ŋ߂̃t�@�C����ۑ����܂���: {Count}��", recentFilesData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋ߂̃t�@�C���ۑ����ɃG���[������");
            }
        }
    }
}