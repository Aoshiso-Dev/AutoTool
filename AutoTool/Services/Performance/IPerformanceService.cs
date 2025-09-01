using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AutoTool.Services.Performance
{
    /// <summary>
    /// �p�t�H�[�}���X�Ď��T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IPerformanceService
    {
        /// <summary>
        /// ����̎��s���Ԃ𑪒�
        /// </summary>
        Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation);

        /// <summary>
        /// ����̎��s���Ԃ𑪒�i�߂�l�Ȃ��j
        /// </summary>
        Task MeasureAsync(string operationName, Func<Task> operation);

        /// <summary>
        /// ���g���N�X���L�^
        /// </summary>
        void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// �J�E���^�[�𑝉�
        /// </summary>
        void IncrementCounter(string counterName, Dictionary<string, string>? tags = null);

        /// <summary>
        /// ���݂̃��g���N�X�T�v���擾
        /// </summary>
        PerformanceStatistics GetStatistics();

        /// <summary>
        /// ���g���N�X���N���A
        /// </summary>
        void ClearMetrics();

        /// <summary>
        /// �p�t�H�[�}���X�Ď����J�n
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// �p�t�H�[�}���X�Ď����~
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// ���݂̃p�t�H�[�}���X�����擾
        /// </summary>
        PerformanceInfo GetCurrentInfo();
    }

    /// <summary>
    /// �p�t�H�[�}���X���v���
    /// </summary>
    public class PerformanceStatistics
    {
        public Dictionary<string, OperationStatistics> Operations { get; } = new();
        public Dictionary<string, double> Metrics { get; } = new();
        public Dictionary<string, long> Counters { get; } = new();
        public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// ���쓝�v���
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
    /// ���݂̃p�t�H�[�}���X���
    /// </summary>
    public class PerformanceInfo
    {
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}