using System;
using System.IO;

namespace AutoTool.Services.Validation
{
    /// <summary>
    /// 設定値やパラメータの検証を行うヘルパークラス
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 必須項目の検証
        /// </summary>
        public static void ValidateRequired(string value, string parameterName, string displayName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                var message = string.IsNullOrEmpty(displayName) 
                    ? $"{parameterName} は必須項目です"
                    : $"{displayName} は必須項目です";
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// 閾値の検証（0.0-1.0）
        /// </summary>
        public static void ValidateThreshold(double value, string parameterName, string displayName = "閾値")
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, 
                    $"{displayName} は0.0から1.0の範囲で指定してください");
            }
        }

        /// <summary>
        /// 正の整数の検証
        /// </summary>
        public static void ValidatePositiveInteger(int value, string parameterName, string displayName = "")
        {
            if (value <= 0)
            {
                var message = string.IsNullOrEmpty(displayName)
                    ? $"{parameterName} は正の整数である必要があります"
                    : $"{displayName} は正の整数である必要があります";
                throw new ArgumentOutOfRangeException(parameterName, value, message);
            }
        }

        /// <summary>
        /// 変数名の検証
        /// </summary>
        public static void ValidateVariableName(string value, string parameterName)
        {
            ValidateRequired(value, parameterName, "変数名");

            // 変数名の形式チェック（英数字とアンダースコアのみ、先頭は英字）
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException("変数名は英字で始まり、英数字とアンダースコアのみ使用可能です", parameterName);
            }
        }

        /// <summary>
        /// ファイルパスの検証
        /// </summary>
        public static void ValidateFilePath(string filePath, string parameterName, bool mustExist = true)
        {
            ValidateRequired(filePath, parameterName, "ファイルパス");

            if (mustExist && !File.Exists(filePath))
            {
                throw new FileNotFoundException($"指定されたファイルが見つかりません: {filePath}", filePath);
            }

            // 無効な文字のチェック
            var invalidChars = Path.GetInvalidPathChars();
            if (filePath.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException("ファイルパスに無効な文字が含まれています", parameterName);
            }
        }

        /// <summary>
        /// ディレクトリパスの検証
        /// </summary>
        public static void ValidateDirectoryPath(string directoryPath, string parameterName, bool mustExist = true)
        {
            ValidateRequired(directoryPath, parameterName, "ディレクトリパス");

            if (mustExist && !Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定されたディレクトリが見つかりません: {directoryPath}");
            }
        }

        /// <summary>
        /// 範囲の検証
        /// </summary>
        public static void ValidateRange<T>(T value, T min, T max, string parameterName, string displayName = "") 
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                var message = string.IsNullOrEmpty(displayName)
                    ? $"{parameterName} は {min} から {max} の範囲で指定してください"
                    : $"{displayName} は {min} から {max} の範囲で指定してください";
                throw new ArgumentOutOfRangeException(parameterName, value, message);
            }
        }

        /// <summary>
        /// タイムアウト値の検証
        /// </summary>
        public static void ValidateTimeout(int timeoutMs, string parameterName = "timeoutMs")
        {
            ValidateRange(timeoutMs, 100, 300000, parameterName, "タイムアウト"); // 100ms - 5分
        }

        /// <summary>
        /// インターバル値の検証
        /// </summary>
        public static void ValidateInterval(int intervalMs, string parameterName = "intervalMs")
        {
            ValidateRange(intervalMs, 10, 60000, parameterName, "インターバル"); // 10ms - 1分
        }
    }

    /// <summary>
    /// 検証可能なオブジェクトのインターフェース
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// オブジェクトの検証を行う
        /// </summary>
        void Validate();
    }

    /// <summary>
    /// 検証エラーの詳細情報
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public object? AttemptedValue { get; set; }

        public ValidationError() { }

        public ValidationError(string propertyName, string errorMessage, object? attemptedValue = null)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            AttemptedValue = attemptedValue;
        }

        public override string ToString()
        {
            return $"{PropertyName}: {ErrorMessage}";
        }
    }

    /// <summary>
    /// 複数の検証エラーを含む例外
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationError[] Errors { get; }

        public ValidationException(params ValidationError[] errors) 
            : base(CreateMessage(errors))
        {
            Errors = errors ?? Array.Empty<ValidationError>();
        }

        public ValidationException(string message, params ValidationError[] errors) 
            : base(message)
        {
            Errors = errors ?? Array.Empty<ValidationError>();
        }

        private static string CreateMessage(ValidationError[] errors)
        {
            if (errors == null || errors.Length == 0)
                return "検証エラーが発生しました";

            if (errors.Length == 1)
                return $"検証エラー: {errors[0]}";

            return $"検証エラー（{errors.Length}件）:\n" + string.Join("\n", Array.ConvertAll(errors, e => e.ToString()));
        }
    }
}