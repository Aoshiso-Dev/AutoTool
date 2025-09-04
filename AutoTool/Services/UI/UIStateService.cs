using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// UI��Ԃ��ꌳ�Ǘ�����T�[�r�X
    /// </summary>
    public interface IUIStateService
    {
        // �E�B���h�E���
        string Title { get; set; }
        double WindowWidth { get; set; }
        double WindowHeight { get; set; }
        System.Windows.WindowState WindowState { get; set; }
        
        // �A�v���P�[�V�������
        bool IsLoading { get; set; }
        bool IsRunning { get; set; }
        string StatusMessage { get; set; }
        
        // �p�t�H�[�}���X���
        string MemoryUsage { get; set; }
        string CpuUsage { get; set; }
        
        // �R�}���h�֘A
        int CommandCount { get; set; }
        int PluginCount { get; set; }
        
        // ���O
        ObservableCollection<string> LogEntries { get; }
        
        // ���\�b�h
        void AddLogEntry(string message);
        void ClearLog();
        void SaveWindowSettings();
        void LoadWindowSettings();
    }

    /// <summary>
    /// UI��ԊǗ��T�[�r�X�̎����i�ݒ�A�g�����Łj
    /// </summary>
    public partial class UIStateService : ObservableObject, IUIStateService
    {
        private readonly ILogger<UIStateService> _logger;
        private readonly IEnhancedConfigurationService _configService;

        [ObservableProperty]
        private string _title = "AutoTool - �����}�N���������c�[��";
        
        [ObservableProperty]
        private double _windowWidth = 1200;
        
        [ObservableProperty]
        private double _windowHeight = 800;
        
        [ObservableProperty]
        private System.Windows.WindowState _windowState = System.Windows.WindowState.Normal;
        
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
        private int _commandCount = 0;
        
        [ObservableProperty]
        private int _pluginCount = 0;

        public ObservableCollection<string> LogEntries { get; } = new();

        public UIStateService(ILogger<UIStateService> logger, IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            // �������O�G���g��
            AddLogEntry("AutoTool UI����������");
            AddLogEntry("UIStateService��������");
            
            // �ݒ肩�珉���l��ǂݍ���
            LoadWindowSettings();
            
            _logger.LogInformation("UIStateService����������");
        }

        public void AddLogEntry(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            
            LogEntries.Add(logEntry);
            
            // ���O�G���g������������ꍇ�͌Â����̂��폜
            if (LogEntries.Count > 1000)
            {
                for (int i = 0; i < 100; i++)
                {
                    LogEntries.RemoveAt(0);
                }
            }
        }

        public void ClearLog()
        {
            LogEntries.Clear();
            AddLogEntry("���O�N���A");
            _logger.LogDebug("���O���N���A���܂���");
        }

        public void SaveWindowSettings()
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.UI.WindowWidth, WindowWidth);
                _configService.SetValue(ConfigurationKeys.UI.WindowHeight, WindowHeight);
                _configService.SetValue(ConfigurationKeys.UI.WindowState, WindowState.ToString());
                _configService.Save();
                
                _logger.LogDebug("�E�B���h�E�ݒ�ۑ�����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E�ݒ�ۑ����ɃG���[���������܂���");
            }
        }

        public void LoadWindowSettings()
        {
            try
            {
                WindowWidth = _configService.GetValue(ConfigurationKeys.UI.WindowWidth, 1200.0);
                WindowHeight = _configService.GetValue(ConfigurationKeys.UI.WindowHeight, 800.0);
                
                var windowStateStr = _configService.GetValue(ConfigurationKeys.UI.WindowState, "Normal");
                if (Enum.TryParse<System.Windows.WindowState>(windowStateStr, out var windowState))
                {
                    WindowState = windowState;
                }
                
                _logger.LogDebug("�E�B���h�E�ݒ�ǂݍ��݊���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E�ݒ�ǂݍ��ݒ��ɃG���[���������܂���");
            }
        }

        partial void OnIsRunningChanged(bool value)
        {
            StatusMessage = value ? "���s��..." : "��������";
            _logger.LogDebug("���s��ԕύX: {IsRunning}", value);
        }

        partial void OnCommandCountChanged(int value)
        {
            _logger.LogTrace("�R�}���h���ύX: {Count}", value);
        }

        // �E�B���h�E�T�C�Y�ύX���Ɏ����ۑ�
        partial void OnWindowWidthChanged(double value)
        {
            SaveWindowSettings();
        }

        partial void OnWindowHeightChanged(double value)
        {
            SaveWindowSettings();
        }

        partial void OnWindowStateChanged(System.Windows.WindowState value)
        {
            SaveWindowSettings();
        }
    }
}