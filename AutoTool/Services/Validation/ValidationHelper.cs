using System;
using System.IO;

namespace AutoTool.Services.Validation
{
    /// <summary>
    /// �ݒ�l��p�����[�^�̌��؂��s���w���p�[�N���X
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// �K�{���ڂ̌���
        /// </summary>
        public static void ValidateRequired(string value, string parameterName, string displayName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                var message = string.IsNullOrEmpty(displayName) 
                    ? $"{parameterName} �͕K�{���ڂł�"
                    : $"{displayName} �͕K�{���ڂł�";
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// 臒l�̌��؁i0.0-1.0�j
        /// </summary>
        public static void ValidateThreshold(double value, string parameterName, string displayName = "臒l")
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, 
                    $"{displayName} ��0.0����1.0�͈̔͂Ŏw�肵�Ă�������");
            }
        }

        /// <summary>
        /// ���̐����̌���
        /// </summary>
        public static void ValidatePositiveInteger(int value, string parameterName, string displayName = "")
        {
            if (value <= 0)
            {
                var message = string.IsNullOrEmpty(displayName)
                    ? $"{parameterName} �͐��̐����ł���K�v������܂�"
                    : $"{displayName} �͐��̐����ł���K�v������܂�";
                throw new ArgumentOutOfRangeException(parameterName, value, message);
            }
        }

        /// <summary>
        /// �ϐ����̌���
        /// </summary>
        public static void ValidateVariableName(string value, string parameterName)
        {
            ValidateRequired(value, parameterName, "�ϐ���");

            // �ϐ����̌`���`�F�b�N�i�p�����ƃA���_�[�X�R�A�̂݁A�擪�͉p���j
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException("�ϐ����͉p���Ŏn�܂�A�p�����ƃA���_�[�X�R�A�̂ݎg�p�\�ł�", parameterName);
            }
        }

        /// <summary>
        /// �t�@�C���p�X�̌���
        /// </summary>
        public static void ValidateFilePath(string filePath, string parameterName, bool mustExist = true)
        {
            ValidateRequired(filePath, parameterName, "�t�@�C���p�X");

            if (mustExist && !File.Exists(filePath))
            {
                throw new FileNotFoundException($"�w�肳�ꂽ�t�@�C����������܂���: {filePath}", filePath);
            }

            // �����ȕ����̃`�F�b�N
            var invalidChars = Path.GetInvalidPathChars();
            if (filePath.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException("�t�@�C���p�X�ɖ����ȕ������܂܂�Ă��܂�", parameterName);
            }
        }

        /// <summary>
        /// �f�B���N�g���p�X�̌���
        /// </summary>
        public static void ValidateDirectoryPath(string directoryPath, string parameterName, bool mustExist = true)
        {
            ValidateRequired(directoryPath, parameterName, "�f�B���N�g���p�X");

            if (mustExist && !Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"�w�肳�ꂽ�f�B���N�g����������܂���: {directoryPath}");
            }
        }

        /// <summary>
        /// �͈͂̌���
        /// </summary>
        public static void ValidateRange<T>(T value, T min, T max, string parameterName, string displayName = "") 
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                var message = string.IsNullOrEmpty(displayName)
                    ? $"{parameterName} �� {min} ���� {max} �͈̔͂Ŏw�肵�Ă�������"
                    : $"{displayName} �� {min} ���� {max} �͈̔͂Ŏw�肵�Ă�������";
                throw new ArgumentOutOfRangeException(parameterName, value, message);
            }
        }

        /// <summary>
        /// �^�C���A�E�g�l�̌���
        /// </summary>
        public static void ValidateTimeout(int timeoutMs, string parameterName = "timeoutMs")
        {
            ValidateRange(timeoutMs, 100, 300000, parameterName, "�^�C���A�E�g"); // 100ms - 5��
        }

        /// <summary>
        /// �C���^�[�o���l�̌���
        /// </summary>
        public static void ValidateInterval(int intervalMs, string parameterName = "intervalMs")
        {
            ValidateRange(intervalMs, 10, 60000, parameterName, "�C���^�[�o��"); // 10ms - 1��
        }
    }

    /// <summary>
    /// ���؉\�ȃI�u�W�F�N�g�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// �I�u�W�F�N�g�̌��؂��s��
        /// </summary>
        void Validate();
    }

    /// <summary>
    /// ���؃G���[�̏ڍ׏��
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
    /// �����̌��؃G���[���܂ޗ�O
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
                return "���؃G���[���������܂���";

            if (errors.Length == 1)
                return $"���؃G���[: {errors[0]}";

            return $"���؃G���[�i{errors.Length}���j:\n" + string.Join("\n", Array.ConvertAll(errors, e => e.ToString()));
        }
    }
}