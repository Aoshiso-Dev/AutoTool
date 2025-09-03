using System;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// ���،��ʃN���X
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// ���؂������������ǂ���
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// �G���[���b�Z�[�W
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// �x�����b�Z�[�W
        /// </summary>
        public string WarningMessage { get; set; } = string.Empty;

        /// <summary>
        /// �G���[��
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// �x����
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// ���؎��s����
        /// </summary>
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// �ڍ׏��
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// �x�������邩�ǂ���
        /// </summary>
        public bool HasWarnings => WarningCount > 0;

        /// <summary>
        /// �G���[�����邩�ǂ���
        /// </summary>
        public bool HasErrors => ErrorCount > 0;

        /// <summary>
        /// ���S�ɐ����i�G���[���x�����Ȃ��j���ǂ���
        /// </summary>
        public bool IsCompleteSuccess => IsValid && !HasWarnings;
    }
}