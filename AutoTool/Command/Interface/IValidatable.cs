using System;

namespace AutoTool.Command.Interface
{
    /// <summary>
    /// �o���f�[�V�����@�\��񋟂���C���^�[�t�F�[�X
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// �ݒ�l�̃o���f�[�V���������s
        /// </summary>
        /// <exception cref="ArgumentException">�K�{���ڂ��ݒ肳��Ă��Ȃ��ꍇ</exception>
        /// <exception cref="ArgumentOutOfRangeException">�l���͈͊O�̏ꍇ</exception>
        void Validate();
    }

    /// <summary>
    /// �͈̓`�F�b�N�p�̃w���p�[�N���X
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// �l��0-1�͈͓̔����`�F�b�N
        /// </summary>
        public static void ValidateThreshold(double value, string paramName)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName}��0-1�͈̔͂ł���K�v������܂�");
        }

        /// <summary>
        /// �����񂪋�łȂ����`�F�b�N
        /// </summary>
        public static void ValidateRequired(string value, string paramName, string displayName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{displayName}�͕K�{�ł�", paramName);
        }

        /// <summary>
        /// �ϐ����Ƃ��ėL�����`�F�b�N
        /// </summary>
        public static void ValidateVariableName(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("�ϐ����͕K�{�ł�", paramName);
        }

        /// <summary>
        /// ���̐������`�F�b�N
        /// </summary>
        public static void ValidatePositiveInteger(int value, string paramName, string displayName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, $"{displayName}��1�ȏ�ł���K�v������܂�");
        }
    }
}