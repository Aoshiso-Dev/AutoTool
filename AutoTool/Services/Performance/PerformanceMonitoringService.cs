using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Performance
{
    /// <summary>
    /// �A�v���P�[�V�����̃p�t�H�[�}���X�Ď��T�[�r�X
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        string MemoryUsage { get; }
        string CpuUsage { get; }
        double MemoryUsageMB { get; }
        double CpuUsagePercent { get; }
        
        void StartMonitoring();
        void StopMonitoring();
        
        event EventHandler<PerformanceData> PerformanceUpdated;
    }

    /// <summary>
    /// �p�t�H�[�}���X�f�[�^
    /// </summary>
    public record PerformanceData(double MemoryUsageMB, double CpuUsagePercent, DateTime Timestamp);

    /// <summary>
    /// �p�t�H�[�}���X�Ď��T�[�r�X�����i�y�ʔŁj
    /// </summary>
    public class PerformanceMonitoringService : ObservableObject, IPerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly System.Threading.Timer _monitoringTimer;
        private readonly Process _currentProcess;
        private readonly PerformanceCounter? _cpuCounter;
        
        private DateTime _lastCpuTime;
        private TimeSpan _lastTotalProcessorTime;
        private volatile bool _isMonitoring;

        private string _memoryUsage = "0 MB";
        private string _cpuUsage = "0%";
        private double _memoryUsageMB;
        private double _cpuUsagePercent;

        public string MemoryUsage 
        { 
            get => _memoryUsage; 
            private set => SetProperty(ref _memoryUsage, value); 
        }
        
        public string CpuUsage 
        { 
            get => _cpuUsage; 
            private set => SetProperty(ref _cpuUsage, value); 
        }
        
        public double MemoryUsageMB 
        { 
            get => _memoryUsageMB; 
            private set => SetProperty(ref _memoryUsageMB, value); 
        }
        
        public double CpuUsagePercent 
        { 
            get => _cpuUsagePercent; 
            private set => SetProperty(ref _cpuUsagePercent, value); 
        }

        public event EventHandler<PerformanceData>? PerformanceUpdated;

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentProcess = Process.GetCurrentProcess();
            
            // CPU�J�E���^�̏������i���s����null�j
            try
            {
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _currentProcess.ProcessName, true);
                _cpuCounter.NextValue(); // ����͎̂Ă�i�l�͖����j
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPU�J�E���^�̏������Ɏ��s���܂����BCPU�J�E���^�͖����ɂȂ�܂��B");
                _cpuCounter = null;
            }

            // �^�C�}�[�i5�b�Ԋu�j
            _monitoringTimer = new System.Threading.Timer(UpdatePerformanceMetrics, null, Timeout.Infinite, 5000);
            
            _lastCpuTime = DateTime.UtcNow;
            _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            
            _isMonitoring = true;
            _monitoringTimer.Change(0, 5000); // �����ɊJ�n�A5�b�Ԋu
            _logger.LogDebug("�p�t�H�[�}���X�Ď��J�n");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;
            
            _isMonitoring = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogDebug("�p�t�H�[�}���X�Ď���~");
        }

        private void UpdatePerformanceMetrics(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                // �������g�p�ʂ̎擾�i�y�ʁj
                _currentProcess.Refresh();
                var memoryMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                
                // CPU�g�p���̎擾�i�y�ʌv�Z�j
                var cpuPercent = CalculateCpuUsage();

                // �v���p�e�B�X�V�iUI�X���b�h�Ŏ��s�j
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    MemoryUsageMB = memoryMB;
                    MemoryUsage = $"{memoryMB:F1} MB";
                    
                    CpuUsagePercent = cpuPercent;
                    CpuUsage = $"{cpuPercent:F1}%";
                });

                // �C�x���g����
                PerformanceUpdated?.Invoke(this, new PerformanceData(memoryMB, cpuPercent, DateTime.Now));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�p�t�H�[�}���X���g���N�X�X�V���ɃG���[���������܂���");
            }
        }

        private double CalculateCpuUsage()
        {
            try
            {
                // PerformanceCounter�����p�\�ȏꍇ�͂�����g�p
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }

                // �t�H�[���o�b�N: �蓮�v�Z�i�y�ʁj
                var currentTime = DateTime.UtcNow;
                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

                var timeDiff = currentTime - _lastCpuTime;
                var processorTimeDiff = currentTotalProcessorTime - _lastTotalProcessorTime;

                var cpuUsage = processorTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds / Environment.ProcessorCount * 100;

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                return Math.Max(0, Math.Min(100, cpuUsage)); // 0-100%�ɐ��K��
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPU�g�p���v�Z���ɃG���[���������܂���");
                return 0;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
}