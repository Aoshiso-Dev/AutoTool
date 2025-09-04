using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// UI状態を一元管理するサービス
    /// </summary>
    public interface IUIStateService
    {
        // ウィンドウ状態
        string Title { get; set; }
        double WindowWidth { get; set; }
        double WindowHeight { get; set; }
        System.Windows.WindowState WindowState { get; set; }
        
        // アプリケーション状態
        bool IsLoading { get; set; }
        bool IsRunning { get; set; }
        string StatusMessage { get; set; }
        
        // パフォーマンス情報
        string MemoryUsage { get; set; }
        string CpuUsage { get; set; }
        
        // コマンド関連
        int CommandCount { get; set; }
        int PluginCount { get; set; }
        
        // ログ
        ObservableCollection<string> LogEntries { get; }
        
        // メソッド
        void AddLogEntry(string message);
        void ClearLog();
        void SaveWindowSettings();
        void LoadWindowSettings();
    }

    /// <summary>
    /// UI状態管理サービスの実装（設定連携強化版）
    /// </summary>
    public partial class UIStateService : ObservableObject, IUIStateService
    {
        private readonly ILogger<UIStateService> _logger;
        private readonly IEnhancedConfigurationService _configService;

        [ObservableProperty]
        private string _title = "AutoTool - 統合マクロ自動化ツール";
        
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
        private string _statusMessage = "準備完了";
        
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
            
            // 初期ログエントリ
            AddLogEntry("AutoTool UI初期化完了");
            AddLogEntry("UIStateService準備完了");
            
            // 設定から初期値を読み込み
            LoadWindowSettings();
            
            _logger.LogInformation("UIStateService初期化完了");
        }

        public void AddLogEntry(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            
            LogEntries.Add(logEntry);
            
            // ログエントリが多すぎる場合は古いものを削除
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
            AddLogEntry("ログクリア");
            _logger.LogDebug("ログをクリアしました");
        }

        public void SaveWindowSettings()
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.UI.WindowWidth, WindowWidth);
                _configService.SetValue(ConfigurationKeys.UI.WindowHeight, WindowHeight);
                _configService.SetValue(ConfigurationKeys.UI.WindowState, WindowState.ToString());
                _configService.Save();
                
                _logger.LogDebug("ウィンドウ設定保存完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定保存中にエラーが発生しました");
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
                
                _logger.LogDebug("ウィンドウ設定読み込み完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定読み込み中にエラーが発生しました");
            }
        }

        partial void OnIsRunningChanged(bool value)
        {
            StatusMessage = value ? "実行中..." : "準備完了";
            _logger.LogDebug("実行状態変更: {IsRunning}", value);
        }

        partial void OnCommandCountChanged(int value)
        {
            _logger.LogTrace("コマンド数変更: {Count}", value);
        }

        // ウィンドウサイズ変更時に自動保存
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