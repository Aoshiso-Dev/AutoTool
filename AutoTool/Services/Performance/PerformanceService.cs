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
    /// パフォーマンス監視サービスの実装
    /// </summary>
    public class PerformanceService : IPerformanceService
    {
        private readonly ILogger<PerformanceService> _logger;
        private readonly ConcurrentDictionary<string, OperationStatistics> _operations;
        private readonly ConcurrentDictionary<string, double> _metrics;
        private readonly ConcurrentDictionary<string, long> _counters;
        private readonly object _lockObject = new();
        
        // パフォーマンス監視用
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

            // CPUカウンターの初期化（Windowsでのみ）
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CPUカウンターの初期化に失敗しました");
            }

            _logger.LogInformation("PerformanceService 初期化完了");
        }

        /// <summary>
        /// パフォーマンス監視を開始
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            try
            {
                _isMonitoring = true;
                _monitoringTimer = new System.Threading.Timer(UpdatePerformanceMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
                _logger.LogInformation("パフォーマンス監視を開始しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "パフォーマンス監視開始中にエラーが発生しました");
                _isMonitoring = false;
            }
        }

        /// <summary>
        /// パフォーマンス監視を停止
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
                _logger.LogInformation("パフォーマンス監視を停止しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "パフォーマンス監視停止中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 現在のパフォーマンス情報を取得
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
                _logger.LogError(ex, "パフォーマンス情報取得中にエラーが発生しました");
                return new PerformanceInfo();
            }
        }

        /// <summary>
        /// CPU使用率を取得
        /// </summary>
        private double GetCpuUsage()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }
                
                // フォールバック：プロセス固有のCPU時間から計算
                return _currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 10.0;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "CPU使用率取得中にエラーが発生しました");
                return 0.0;
            }
        }

        /// <summary>
        /// パフォーマンスメトリクスを定期的に更新
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
                _logger.LogTrace(ex, "パフォーマンスメトリクス更新中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 操作の実行時間を測定（戻り値あり）
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
                _logger.LogTrace("操作開始: {OperationName}", operationName);
                result = await operation();
                return result;
            }
            catch (Exception ex)
            {
                thrownException = ex;
                _logger.LogWarning(ex, "操作中に例外が発生: {OperationName}", operationName);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var durationMs = stopwatch.Elapsed.TotalMilliseconds;
                
                RecordOperationMetrics(operationName, durationMs, thrownException == null);
                
                _logger.LogTrace("操作完了: {OperationName}, 実行時間: {Duration}ms, 成功: {Success}",
                    operationName, durationMs, thrownException == null);
            }
        }

        /// <summary>
        /// 操作の実行時間を測定（戻り値なし）
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
        /// メトリクスを記録
        /// </summary>
        public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            try
            {
                var key = CreateMetricKey(metricName, tags);
                _metrics.AddOrUpdate(key, value, (k, v) => value);

                _logger.LogTrace("メトリクス記録: {MetricName} = {Value}, Tags: {Tags}",
                    metricName, value, tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メトリクス記録中にエラー発生: {MetricName}", metricName);
            }
        }

        /// <summary>
        /// カウンターを増加
        /// </summary>
        public void IncrementCounter(string counterName, Dictionary<string, string>? tags = null)
        {
            if (string.IsNullOrWhiteSpace(counterName))
                throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));

            try
            {
                var key = CreateMetricKey(counterName, tags);
                _counters.AddOrUpdate(key, 1, (k, v) => v + 1);

                _logger.LogTrace("カウンター増加: {CounterName}, Tags: {Tags}",
                    counterName, tags != null ? string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "カウンター増加中にエラー発生: {CounterName}", counterName);
            }
        }

        /// <summary>
        /// 現在のメトリクス概要を取得
        /// </summary>
        public PerformanceStatistics GetStatistics()
        {
            try
            {
                lock (_lockObject)
                {
                    var statistics = new PerformanceStatistics();

                    // 操作統計をコピー
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

                    // メトリクスをコピー
                    foreach (var kvp in _metrics)
                    {
                        statistics.Metrics[kvp.Key] = kvp.Value;
                    }

                    // カウンターをコピー
                    foreach (var kvp in _counters)
                    {
                        statistics.Counters[kvp.Key] = kvp.Value;
                    }

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "統計取得中にエラーが発生しました");
                return new PerformanceStatistics();
            }
        }

        /// <summary>
        /// メトリクスをクリア
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

                _logger.LogInformation("全てのメトリクスをクリアしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メトリクスクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 操作メトリクスを記録
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

                // 成功/失敗カウンターを更新
                var counterName = isSuccess ? $"{operationName}.Success" : $"{operationName}.Failure";
                IncrementCounter(counterName);

                // 実行時間メトリクスを記録
                RecordMetric($"{operationName}.Duration", durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "操作メトリクス記録中にエラー発生: {OperationName}", operationName);
            }
        }

        /// <summary>
        /// タグ付きメトリクスキーを作成
        /// </summary>
        private static string CreateMetricKey(string name, Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return name;

            var tagString = string.Join(",", tags.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{name}[{tagString}]";
        }

        /// <summary>
        /// 統計サマリーを文字列として取得（デバッグ用）
        /// </summary>
        public string GetStatisticsSummary()
        {
            try
            {
                var stats = GetStatistics();
                var summary = new List<string>
                {
                    "=== パフォーマンス概要 ===",
                    $"最終更新: {stats.LastResetTime:yyyy-MM-dd HH:mm:ss} UTC",
                    "",
                    "操作統計:"
                };

                foreach (var op in stats.Operations.Values.OrderByDescending(o => o.ExecutionCount))
                {
                    summary.Add($"  {op.OperationName}:");
                    summary.Add($"    実行回数: {op.ExecutionCount}");
                    summary.Add($"    平均時間: {op.AverageDurationMs:F2}ms");
                    summary.Add($"    最小時間: {op.MinDurationMs:F2}ms");
                    summary.Add($"    最大時間: {op.MaxDurationMs:F2}ms");
                    summary.Add($"    最終実行: {op.LastExecutedAt:yyyy-MM-dd HH:mm:ss}");
                    summary.Add("");
                }

                if (stats.Counters.Count > 0)
                {
                    summary.Add("カウンター:");
                    foreach (var counter in stats.Counters.OrderByDescending(c => c.Value))
                    {
                        summary.Add($"  {counter.Key}: {counter.Value}");
                    }
                }

                return string.Join(Environment.NewLine, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "統計サマリー作成中にエラーが発生しました");
                return "統計サマリー作成エラー";
            }
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
}