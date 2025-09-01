using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AutoTool.Services.Performance
{
    /// <summary>
    /// パフォーマンス監視サービスのインターフェース
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>
        /// 操作の実行時間を測定
        /// </summary>
        Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation);

        /// <summary>
        /// 操作の実行時間を測定（戻り値なし）
        /// </summary>
        Task MeasureAsync(string operationName, Func<Task> operation);

        /// <summary>
        /// メトリクスを記録
        /// </summary>
        void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// カウンターを増加
        /// </summary>
        void IncrementCounter(string counterName, Dictionary<string, string>? tags = null);

        /// <summary>
        /// 現在のメトリクス概要を取得
        /// </summary>
        PerformanceStatistics GetStatistics();

        /// <summary>
        /// メトリクスをクリア
        /// </summary>
        void ClearMetrics();

        /// <summary>
        /// パフォーマンス監視を開始
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// パフォーマンス監視を停止
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 現在のパフォーマンス情報を取得
        /// </summary>
        PerformanceInfo GetCurrentInfo();
    }

    /// <summary>
    /// パフォーマンス統計情報
    /// </summary>
    public class PerformanceStatistics
    {
        public Dictionary<string, OperationStatistics> Operations { get; } = new();
        public Dictionary<string, double> Metrics { get; } = new();
        public Dictionary<string, long> Counters { get; } = new();
        public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 操作統計情報
    /// </summary>
    public class OperationStatistics
    {
        public string OperationName { get; set; } = string.Empty;
        public long ExecutionCount { get; set; }
        public double TotalDurationMs { get; set; }
        public double MinDurationMs { get; set; } = double.MaxValue;
        public double MaxDurationMs { get; set; }
        public double AverageDurationMs => ExecutionCount > 0 ? TotalDurationMs / ExecutionCount : 0;
        public DateTime LastExecutedAt { get; set; }
    }

    /// <summary>
    /// 現在のパフォーマンス情報
    /// </summary>
    public class PerformanceInfo
    {
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}