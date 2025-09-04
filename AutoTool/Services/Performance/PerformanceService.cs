using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Performance
{
    /// <summary>
    /// �p�t�H�[�}���X�Ď��T�[�r�X�̎���
    /// </summary>
    public class PerformanceService : IPerformanceService
    {
        private readonly ILogger<PerformanceService> _logger;
        private readonly ConcurrentDictionary<string, OperationStatistics> _operations;
        private readonly ConcurrentDictionary<string, double> _metrics;
        private readonly ConcurrentDictionary<string, long> _counters;
        private readonly object _lockObject = new();
        
        // �p�t�H�[�}���X�Ď��p
        private System.Threading.Timer? _monitoringTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly Process _currentProcess;
        private bool _isMonitoring;

        public PerformanceService(ILogger<PerformanceService> logger)
        {
            _logger = logger;
            _operations = new ConcurrentDictionary<string, OperationStatistics>();
            _metrics = new ConcurrentDictionary<string, double>();
            _counters = new ConcurrentDictionary<string, long>();
            _currentProcess = Process.GetCurrentProcess();

            // CPU�J�E���^�[�̏������iWindows�ł̂݁j
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPU�J�E���^�[�̏������Ɏ��s���܂���");
            }

            _logger.LogInformation("PerformanceService ����������");
        }

        /// <summary>
        /// �p�t�H�[�}���X�Ď����J�n
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            try
            {
                _isMonitoring = true;
                _monitoringTimer = new System.Threading.Timer(UpdatePerformanceMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
                _logger.LogInformation("�p�t�H�[�}���X�Ď����J�n���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�p�t�H�[�}���X�Ď��J�n���ɃG���[���������܂���");
                _isMonitoring = false;
            }
        }

        /// <summary>
        /// �p�t�H�[�}���X�Ď����~
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            try
            {
                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
                _logger.LogInformation("�p�t�H�[�}���X�Ď����~���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�p�t�H�[�}���X�Ď���~���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���݂̃p�t�H�[�}���X�����擾
        /// </summary>
        public PerformanceInfo GetCurrentInfo()
        {
            try
            {
                var memoryUsageMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var cpuUsagePercent = GetCpuUsage();

                return new PerformanceInfo
                {
                    MemoryUsageMB = memoryUsageMB,
                    CpuUsagePercent = cpuUsagePercent,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�p�t�H�[�}���X���擾���ɃG���[���������܂���");
                return new PerformanceInfo();
            }
        }

        /// <summary>
        /// CPU�g�p�����擾
        /// </summary>
        private double GetCpuUsage()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }
                
                // �t�H�[���o�b�N�F�v���Z�X�ŗL��CPU���Ԃ���v�Z
                return _currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 10.0;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "CPU�g�p���擾���ɃG���[���������܂���");
                return 0.0;
            }
        }

        /// <summary>
        /// �p�t�H�[�}���X���g���N�X�����I�ɍX�V
        /// </summary>
        private void UpdatePerformanceMetrics(object? state)
        {
            try
            {
                var info = GetCurrentInfo();
                RecordMetric("System.MemoryUsageMB", info.MemoryUsageMB);
                RecordMetric("System.CpuUsagePercent", info.CpuUsagePercent);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "�p�t�H�[�}���X���g���N�X�X�V���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ����̎��s���Ԃ𑪒�i�߂�l����j
        /// </summary>
        public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var stopwatch = Stopwatch.StartNew();
            Exception? thrownException = null;
            T result = default(T);

            try
            {
                _logger.LogTrace("����J�n: {OperationName}", operationName);
                result = await operation();
                return result;
            }
            catch (Exception ex)
            {
                thrownException = ex;
                _logger.LogWarning(ex, "���쒆�ɗ�O������: {OperationName}", operationName);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var durationMs = stopwatch.Elapsed.TotalMilliseconds;
                
                RecordOperationMetrics(operationName, durationMs, thrownException == null);
                
                _logger.LogTrace("���슮��: {OperationName}, ���s����: {Duration}ms, ����: {Success}",
                    operationName, durationMs, thrownException == null);
            }
        }

        /// <summary>
        /// ����̎��s���Ԃ𑪒�i�߂�l�Ȃ��j
        /// </summary>
        public async Task MeasureAsync(string operationName, Func<Task> operation)
        {
            await MeasureAsync<object?>(operationName, async () =>
            {
                await operation();
                return null;
            });
        }

        /// <summary>
        /// ���g���N�X���L�^
        /// </summary>
        public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            try
            {
                var key = CreateMetricKey(metricName, tags);
                _metrics.AddOrUpdate(key, value, (k, v) => value);

                _logger.LogTrace("���g���N�X�L�^: {MetricName} = {Value}, Tags: {Tags}",
                    metricName, value, tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���g���N�X�L�^���ɃG���[����: {MetricName}", metricName);
            }
        }

        /// <summary>
        /// �J�E���^�[�𑝉�
        /// </summary>
        public void IncrementCounter(string counterName, Dictionary<string, string>? tags = null)
        {
            if (string.IsNullOrWhiteSpace(counterName))
                throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));

            try
            {
                var key = CreateMetricKey(counterName, tags);
                _counters.AddOrUpdate(key, 1, (k, v) => v + 1);

                _logger.LogTrace("�J�E���^�[����: {CounterName}, Tags: {Tags}",
                    counterName, tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�J�E���^�[�������ɃG���[����: {CounterName}", counterName);
            }
        }

        /// <summary>
        /// ���݂̃��g���N�X�T�v���擾
        /// </summary>
        public PerformanceStatistics GetStatistics()
        {
            try
            {
                lock (_lockObject)
                {
                    var statistics = new PerformanceStatistics();

                    // ���쓝�v���R�s�[
                    foreach (var kvp in _operations)
                    {
                        statistics.Operations[kvp.Key] = new OperationStatistics
                        {
                            OperationName = kvp.Value.OperationName,
                            ExecutionCount = kvp.Value.ExecutionCount,
                            TotalDurationMs = kvp.Value.TotalDurationMs,
                            MinDurationMs = kvp.Value.MinDurationMs == double.MaxValue ? 0 : kvp.Value.MinDurationMs,
                            MaxDurationMs = kvp.Value.MaxDurationMs,
                            LastExecutedAt = kvp.Value.LastExecutedAt
                        };
                    }

                    // ���g���N�X���R�s�[
                    foreach (var kvp in _metrics)
                    {
                        statistics.Metrics[kvp.Key] = kvp.Value;
                    }

                    // �J�E���^�[���R�s�[
                    foreach (var kvp in _counters)
                    {
                        statistics.Counters[kvp.Key] = kvp.Value;
                    }

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���v�擾���ɃG���[���������܂���");
                return new PerformanceStatistics();
            }
        }

        /// <summary>
        /// ���g���N�X���N���A
        /// </summary>
        public void ClearMetrics()
        {
            try
            {
                lock (_lockObject)
                {
                    _operations.Clear();
                    _metrics.Clear();
                    _counters.Clear();
                }

                _logger.LogInformation("�S�Ẵ��g���N�X���N���A���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���g���N�X�N���A���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���상�g���N�X���L�^
        /// </summary>
        private void RecordOperationMetrics(string operationName, double durationMs, bool isSuccess)
        {
            try
            {
                _operations.AddOrUpdate(operationName,
                    new OperationStatistics
                    {
                        OperationName = operationName,
                        ExecutionCount = 1,
                        TotalDurationMs = durationMs,
                        MinDurationMs = durationMs,
                        MaxDurationMs = durationMs,
                        LastExecutedAt = DateTime.UtcNow
                    },
                    (key, existing) =>
                    {
                        existing.ExecutionCount++;
                        existing.TotalDurationMs += durationMs;
                        existing.MinDurationMs = Math.Min(existing.MinDurationMs, durationMs);
                        existing.MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs);
                        existing.LastExecutedAt = DateTime.UtcNow;
                        return existing;
                    });

                // ����/���s�J�E���^�[���X�V
                var counterName = isSuccess ? $"{operationName}.Success" : $"{operationName}.Failure";
                IncrementCounter(counterName);

                // ���s���ԃ��g���N�X���L�^
                RecordMetric($"{operationName}.Duration", durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���상�g���N�X�L�^���ɃG���[����: {OperationName}", operationName);
            }
        }

        /// <summary>
        /// �^�O�t�����g���N�X�L�[���쐬
        /// </summary>
        private static string CreateMetricKey(string name, Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return name;

            var tagString = string.Join(",", tags.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{name}[{tagString}]";
        }

        /// <summary>
        /// ���v�T�}���[�𕶎���Ƃ��Ď擾�i�f�o�b�O�p�j
        /// </summary>
        public string GetStatisticsSummary()
        {
            try
            {
                var stats = GetStatistics();
                var summary = new List<string>
                {
                    "=== �p�t�H�[�}���X�T�v ===",
                    $"�ŏI�X�V: {stats.LastResetTime:yyyy-MM-dd HH:mm:ss} UTC",
                    "",
                    "���쓝�v:"
                };

                foreach (var op in stats.Operations.Values.OrderByDescending(o => o.ExecutionCount))
                {
                    summary.Add($"  {op.OperationName}:");
                    summary.Add($"    ���s��: {op.ExecutionCount}");
                    summary.Add($"    ���ώ���: {op.AverageDurationMs:F2}ms");
                    summary.Add($"    �ŏ�����: {op.MinDurationMs:F2}ms");
                    summary.Add($"    �ő厞��: {op.MaxDurationMs:F2}ms");
                    summary.Add($"    �ŏI���s: {op.LastExecutedAt:yyyy-MM-dd HH:mm:ss}");
                    summary.Add("");
                }

                if (stats.Counters.Count > 0)
                {
                    summary.Add("�J�E���^�[:");
                    foreach (var counter in stats.Counters.OrderByDescending(c => c.Value))
                    {
                        summary.Add($"  {counter.Key}: {counter.Value}");
                    }
                }

                return string.Join(Environment.NewLine, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���v�T�}���[�쐬���ɃG���[���������܂���");
                return "���v�T�}���[�쐬�G���[";
            }
        }

        /// <summary>
        /// ���\�[�X�̉��
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
}