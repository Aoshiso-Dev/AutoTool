using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.UI;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// ウィンドウ設定の永続化を管理するサービス
    /// </summary>
    public interface IWindowSettingsService
    {
        void SaveSettings(double width, double height, System.Windows.WindowState state);
        (double width, double height, System.Windows.WindowState state) LoadSettings();
        void SaveGridSplitterPosition(double position);
        double LoadGridSplitterPosition();
        
        event EventHandler<WindowSettingsChangedEventArgs> SettingsChanged;
    }

    /// <summary>
    /// ウィンドウ設定変更イベント引数
    /// </summary>
    public class WindowSettingsChangedEventArgs : EventArgs
    {
        public double Width { get; }
        public double Height { get; }
        public System.Windows.WindowState State { get; }

        public WindowSettingsChangedEventArgs(double width, double height, System.Windows.WindowState state)
        {
            Width = width;
            Height = height;
            State = state;
        }
    }

    /// <summary>
    /// ウィンドウ設定サービスの実装
    /// </summary>
    public class WindowSettingsService : IWindowSettingsService
    {
        private readonly ILogger<WindowSettingsService> _logger;
        private readonly IEnhancedConfigurationService _configService;

        public event EventHandler<WindowSettingsChangedEventArgs>? SettingsChanged;

        public WindowSettingsService(ILogger<WindowSettingsService> logger, IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            // 設定変更の監視
            _configService.ConfigurationChanged += OnConfigurationChanged;
        }

        private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            // ウィンドウ関連設定が変更された場合にイベント発火
            if (e.Key.StartsWith("UI:Window"))
            {
                var (width, height, state) = LoadSettings();
                SettingsChanged?.Invoke(this, new WindowSettingsChangedEventArgs(width, height, state));
            }
        }

        public void SaveSettings(double width, double height, System.Windows.WindowState state)
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.UI.WindowWidth, width);
                _configService.SetValue(ConfigurationKeys.UI.WindowHeight, height);
                _configService.SetValue(ConfigurationKeys.UI.WindowState, state.ToString());
                _configService.Save();

                _logger.LogDebug("ウィンドウ設定保存: {Width}x{Height}, State={State}", width, height, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定保存エラー");
            }
        }

        public (double width, double height, System.Windows.WindowState state) LoadSettings()
        {
            try
            {
                var width = _configService.GetValue(ConfigurationKeys.UI.WindowWidth, 1200.0);
                var height = _configService.GetValue(ConfigurationKeys.UI.WindowHeight, 800.0);
                var stateStr = _configService.GetValue(ConfigurationKeys.UI.WindowState, "Normal");

                var state = System.Windows.WindowState.Normal;
                if (Enum.TryParse<System.Windows.WindowState>(stateStr, out var parsedState))
                {
                    state = parsedState;
                }

                _logger.LogDebug("ウィンドウ設定読み込み: {Width}x{Height}, State={State}", width, height, state);
                return (width, height, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定読み込みエラー、デフォルト値使用");
                return (1200.0, 800.0, System.Windows.WindowState.Normal);
            }
        }

        public void SaveGridSplitterPosition(double position)
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.UI.GridSplitterPosition, position);
                _configService.Save();

                _logger.LogTrace("GridSplitter位置保存: {Position}", position);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GridSplitter位置保存エラー");
            }
        }

        public double LoadGridSplitterPosition()
        {
            try
            {
                var position = _configService.GetValue(ConfigurationKeys.UI.GridSplitterPosition, 0.5);
                _logger.LogTrace("GridSplitter位置読み込み: {Position}", position);
                return position;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GridSplitter位置読み込みエラー、デフォルト値使用");
                return 0.5;
            }
        }
    }
}