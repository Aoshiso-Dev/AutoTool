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
    /// アプリケーションのパフォーマンス監視サービス
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
    /// パフォーマンスデータ
    /// </summary>
    public record PerformanceData(double MemoryUsageMB, double CpuUsagePercent, DateTime Timestamp);

    /// <summary>
    /// パフォーマンス監視サービス実装（軽量版）
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
            
            // CPUカウンタの初期化（失敗時はnull）
            try
            {
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _currentProcess.ProcessName, true);
                _cpuCounter.NextValue(); // 初回は捨てる（値は無効）
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPUカウンタの初期化に失敗しました。CPUカウンタは無効になります。");
                _cpuCounter = null;
            }

            // タイマー（5秒間隔）
            _monitoringTimer = new System.Threading.Timer(UpdatePerformanceMetrics, null, Timeout.Infinite, 5000);
            
            _lastCpuTime = DateTime.UtcNow;
            _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            
            _isMonitoring = true;
            _monitoringTimer.Change(0, 5000); // 即座に開始、5秒間隔
            _logger.LogDebug("パフォーマンス監視開始");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;
            
            _isMonitoring = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogDebug("パフォーマンス監視停止");
        }

        private void UpdatePerformanceMetrics(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                // メモリ使用量の取得（軽量）
                _currentProcess.Refresh();
                var memoryMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                
                // CPU使用率の取得（軽量計算）
                var cpuPercent = CalculateCpuUsage();

                // プロパティ更新（UIスレッドで実行）
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    MemoryUsageMB = memoryMB;
                    MemoryUsage = $"{memoryMB:F1} MB";
                    
                    CpuUsagePercent = cpuPercent;
                    CpuUsage = $"{cpuPercent:F1}%";
                });

                // イベント発火
                PerformanceUpdated?.Invoke(this, new PerformanceData(memoryMB, cpuPercent, DateTime.Now));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "パフォーマンスメトリクス更新中にエラーが発生しました");
            }
        }

        private double CalculateCpuUsage()
        {
            try
            {
                // PerformanceCounterが利用可能な場合はそれを使用
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }

                // フォールバック: 手動計算（軽量）
                var currentTime = DateTime.UtcNow;
                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

                var timeDiff = currentTime - _lastCpuTime;
                var processorTimeDiff = currentTotalProcessorTime - _lastTotalProcessorTime;

                var cpuUsage = processorTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds / Environment.ProcessorCount * 100;

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                return Math.Max(0, Math.Min(100, cpuUsage)); // 0-100%に正規化
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPU使用率計算中にエラーが発生しました");
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