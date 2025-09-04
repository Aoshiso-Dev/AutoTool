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
}