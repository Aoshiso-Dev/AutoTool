using System;
using System.ComponentModel;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// �o�b�N�O���E���h�N���b�N�����̃A�C�e��
    /// </summary>
    public class BackgroundClickMethodItem
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ���Z�q�̃A�C�e��
    /// </summary>
    public class OperatorItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI���o���[�h�̃A�C�e��
    /// </summary>
    public class AIDetectModeItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}