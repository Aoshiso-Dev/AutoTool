using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFLogPanelViewModel�i���ۂ̃��O�o�͋@�\�t���j
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public partial class LogPanelViewModel : ObservableObject
    {
        private readonly ILogger<LogPanelViewModel> _logger;
        private readonly object _lockObject = new object();

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();

        [ObservableProperty]
        private bool _autoScroll = true;

        [ObservableProperty]
        private int _maxLogEntries = 1000;

        /// <summary>
        /// Phase 5���S�����ŃR���X�g���N�^
        /// </summary>
        public LogPanelViewModel(ILogger<LogPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // �R���N�V�����̕ύX�ʒm��L���ɂ���
            BindingOperations.EnableCollectionSynchronization(_logEntries, _lockObject);

            WriteLog("Phase 5���S������LogPanelViewModel����������");
            _logger.LogInformation("Phase 5���S������LogPanelViewModel����������");
        }

        /// <summary>
        /// ���O�G���g����ǉ�
        /// </summary>
        public void WriteLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";

                lock (_lockObject)
                {
                    // UI�X���b�h�Ŏ��s
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(logEntry);

                        // ���O�G���g�����̐���
                        while (LogEntries.Count > MaxLogEntries)
                        {
                            LogEntries.RemoveAt(0);
                        }
                    });
                }

                _logger.LogDebug("���O�G���g���ǉ�: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�G���g���ǉ����ɃG���[���������܂���: {Message}", message);
            }
        }

        /// <summary>
        /// ���O�G���g����ǉ��i�s�ԍ��ƃR�}���h���t���j
        /// </summary>
        public void WriteLog(string lineNumber, string commandName, string detail)
        {
            var message = $"{lineNumber} {commandName} {detail}";
            WriteLog(message);
        }

        /// <summary>
        /// ���O���N���A
        /// </summary>
        public void ClearLog()
        {
            try
            {
                lock (_lockObject)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Clear();
                    });
                }

                WriteLog("���O���N���A���܂���");
                _logger.LogDebug("���O���N���A���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�N���A���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �ŋ߂̃G���[���O���擾
        /// </summary>
        public System.Collections.Generic.List<string> GetRecentErrorLines()
        {
            try
            {
                lock (_lockObject)
                {
                    return LogEntries
                        .Where(entry => entry.Contains("?") || entry.Contains("�G���[") || entry.Contains("���s"))
                        .TakeLast(5)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�G���[���O�擾���ɃG���[���������܂���");
                return new System.Collections.Generic.List<string>();
            }
        }

        /// <summary>
        /// ���O�t�@�C���ɃG�N�X�|�[�g
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                var allLogs = string.Join(Environment.NewLine, LogEntries);
                System.IO.File.WriteAllText(filePath, allLogs);
                
                WriteLog($"���O�t�@�C���ɃG�N�X�|�[�g����: {filePath}");
                _logger.LogInformation("���O�t�@�C���G�N�X�|�[�g����: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�t�@�C���G�N�X�|�[�g���ɃG���[���������܂���: {FilePath}", filePath);
                WriteLog($"? ���O�t�@�C���G�N�X�|�[�g�G���[: {ex.Message}");
            }
        }

        /// <summary>
        /// ���O�G���g���̓��v�����擾
        /// </summary>
        public (int Total, int Errors, int Warnings) GetLogStatistics()
        {
            try
            {
                lock (_lockObject)
                {
                    var total = LogEntries.Count;
                    var errors = LogEntries.Count(entry => entry.Contains("?") || entry.Contains("�G���["));
                    var warnings = LogEntries.Count(entry => entry.Contains("?") || entry.Contains("�x��"));
                    
                    return (total, errors, warnings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O���v���擾���ɃG���[���������܂���");
                return (0, 0, 0);
            }
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        public void Prepare()
        {
            WriteLog("Phase 5���S����LogPanelViewModel��������");
            _logger.LogDebug("Phase 5���S����LogPanelViewModel��������");
        }
    }
}