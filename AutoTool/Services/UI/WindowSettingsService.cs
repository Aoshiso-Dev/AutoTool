using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.UI;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// �E�B���h�E�ݒ�̉i�������Ǘ�����T�[�r�X
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
    /// �E�B���h�E�ݒ�ύX�C�x���g����
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
    /// �E�B���h�E�ݒ�T�[�r�X�̎���
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

            // �ݒ�ύX�̊Ď�
            _configService.ConfigurationChanged += OnConfigurationChanged;
        }

        private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            // �E�B���h�E�֘A�ݒ肪�ύX���ꂽ�ꍇ�ɃC�x���g����
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

                _logger.LogDebug("�E�B���h�E�ݒ�ۑ�: {Width}x{Height}, State={State}", width, height, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E�ݒ�ۑ��G���[");
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

                _logger.LogDebug("�E�B���h�E�ݒ�ǂݍ���: {Width}x{Height}, State={State}", width, height, state);
                return (width, height, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E�ݒ�ǂݍ��݃G���[�A�f�t�H���g�l�g�p");
                return (1200.0, 800.0, System.Windows.WindowState.Normal);
            }
        }

        public void SaveGridSplitterPosition(double position)
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.UI.GridSplitterPosition, position);
                _configService.Save();

                _logger.LogTrace("GridSplitter�ʒu�ۑ�: {Position}", position);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GridSplitter�ʒu�ۑ��G���[");
            }
        }

        public double LoadGridSplitterPosition()
        {
            try
            {
                var position = _configService.GetValue(ConfigurationKeys.UI.GridSplitterPosition, 0.5);
                _logger.LogTrace("GridSplitter�ʒu�ǂݍ���: {Position}", position);
                return position;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GridSplitter�ʒu�ǂݍ��݃G���[�A�f�t�H���g�l�g�p");
                return 0.5;
            }
        }
    }
}