using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFLogPanelViewModel
    /// </summary>
    public partial class LogPanelViewModel : ObservableObject
    {
        private readonly ILogger<LogPanelViewModel> _logger;
        private readonly StringBuilder _logBuffer = new();
        private const int MAX_LOG_LENGTH = 50000;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _logItems = new();

        [ObservableProperty]
        private int _logEntryCount = 0;

        public LogPanelViewModel(ILogger<LogPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 5������LogPanelViewModel �����������Ă��܂�");
            
            WriteLog("=== ���O�p�l������������ ===");
        }

        /// <summary>
        /// ���O���������݁i�W���`���j
        /// </summary>
        public void WriteLog(string text)
        {
            WriteLog(DateTime.Now.ToString("HH:mm:ss.fff"), "", text);
        }

        /// <summary>
        /// ���O���������݁i�ڍ׌`���j
        /// </summary>
        public void WriteLog(string time, string command, string detail)
        {
            try
            {
                var logEntry = string.IsNullOrEmpty(command) 
                    ? $"[{time}] {detail}"
                    : $"[{time}] {command}: {detail}";
                
                LogItems.Add(logEntry);
                LogEntryCount++;
                
                // ���O����������ꍇ�͌Â��������폜
                if (LogItems.Count > 1000)
                {
                    var itemsToRemove = LogItems.Take(500).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        LogItems.Remove(item);
                    }
                    _logger.LogDebug("���O�A�C�e�����g�������܂���: {Count}�s�ێ�", LogItems.Count);
                }
                
                // �ڍ׃��O�͏���
                if (!detail.Contains("PropertyChanged") && !detail.Contains("�v���p�e�B"))
                {
                    _logger.LogDebug("���O�G���g���ǉ�: {Entry}", logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�������ݒ��ɃG���[���������܂���");
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
            
            if (isRunning)
            {
                WriteLog("=== �}�N�����s�J�n ===");
            }
            else
            {
                WriteLog("=== �}�N�����s�I�� ===");
            }
        }

        /// <summary>
        /// ���O���N���A
        /// </summary>
        public void Clear()
        {
            try
            {
                _logger.LogDebug("���O���蓮�N���A���܂�");
                
                LogItems.Clear();
                LogEntryCount = 0;
                
                WriteLog("=== ���O�N���A ===");
                
                _logger.LogDebug("���O�N���A���������܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���O�N���A���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("LogPanelViewModel �̏������������s���܂�");
                
                // �K�v�ɉ����ď���������ǉ�
                WriteLog("=== ���O�p�l���������� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogPanelViewModel �����������ɃG���[���������܂���");
            }
        }
    }
}