using System;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// 検証結果クラス
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 検証が成功したかどうか
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 警告メッセージ
        /// </summary>
        public string WarningMessage { get; set; } = string.Empty;

        /// <summary>
        /// エラー数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 警告数
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// 検証実行時刻
        /// </summary>
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 詳細情報
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// 警告があるかどうか
        /// </summary>
        public bool HasWarnings => WarningCount > 0;

        /// <summary>
        /// エラーがあるかどうか
        /// </summary>
        public bool HasErrors => ErrorCount > 0;

        /// <summary>
        /// 完全に成功（エラーも警告もない）かどうか
        /// </summary>
        public bool IsCompleteSuccess => IsValid && !HasWarnings;
    }
}