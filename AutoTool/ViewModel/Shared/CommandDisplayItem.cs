using System;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// �R�}���h�\���p�̃A�C�e���N���X�iPhase 3�Ή��j
    /// </summary>
    public class CommandDisplayItem
    {
        public string TypeName { get; init; } = string.Empty;      // �����Ŏg�p����p�ꖼ
        public string DisplayName { get; init; } = string.Empty;   // UI�\���p�̓��{�ꖼ
        public string Category { get; init; } = string.Empty;      // �J�e�S����

        /// <summary>
        /// �f�o�b�O�p�̕�����\���iDisplayName�ł͂Ȃ�TypeName��Ԃ��j
        /// </summary>
        public override string ToString() => $"{DisplayName} ({TypeName})";
        
        public override bool Equals(object? obj)
        {
            return obj is CommandDisplayItem other && TypeName == other.TypeName;
        }
        
        public override int GetHashCode()
        {
            return TypeName.GetHashCode();
        }
    }

    /// <summary>
    /// ���Z�q�A�C�e��
    /// </summary>
    public class OperatorItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI���o���[�h�A�C�e��
    /// </summary>
    public class AIDetectModeItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// �o�b�N�O���E���h�N���b�N�����A�C�e��
    /// </summary>
    public class BackgroundClickMethodItem
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}