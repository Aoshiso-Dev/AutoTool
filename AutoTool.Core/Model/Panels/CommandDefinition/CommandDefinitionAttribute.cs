using System;

namespace AutoTool.Panels.Model.CommandDefinition
{
    /// <summary>
    /// �R�}���h��`�p�̑����B���̑�����t�����N���X���玩���I�ɃR�}���h�^�C�v�����������
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandDefinitionAttribute : Attribute
    {
        /// <summary>
        /// �R�}���h�^�C�v��
        /// </summary>
        public string TypeName { get; }
        
        /// <summary>
        /// �R�}���h�����N���X�^
        /// </summary>
        public Type CommandType { get; }
        
        /// <summary>
        /// �ݒ�C���^�[�t�F�[�X�^
        /// </summary>
        public Type SettingsType { get; }
        
        /// <summary>
        /// �R�}���h����
        /// </summary>
        public CommandCategory Category { get; }
        
        /// <summary>
        /// If���n�R�}���h���ǂ���
        /// </summary>
        public bool IsIfCommand { get; }
        
        /// <summary>
        /// ���[�v�n�R�}���h���ǂ���
        /// </summary>
        public bool IsLoopCommand { get; }
        
        /// <summary>
        /// ネスト終了コマンドかどうか
        /// </summary>
        public bool IsEndCommand { get; }

        /// <summary>
        /// 表示優先度
        /// </summary>
        public int DisplayPriority { get; }

        /// <summary>
        /// 同一優先度内の表示順
        /// </summary>
        public int DisplaySubPriority { get; }

        /// <summary>
        /// 日本語表示名
        /// </summary>
        public string DisplayNameJa { get; }

        /// <summary>
        /// 英語表示名
        /// </summary>
        public string DisplayNameEn { get; }

        public CommandDefinitionAttribute(
            string typeName, 
            Type commandType, 
            Type settingsType, 
            CommandCategory category = CommandCategory.Action,
            bool isIfCommand = false,
            bool isLoopCommand = false,
            bool isEndCommand = false,
            int displayPriority = 9,
            int displaySubPriority = 0,
            string? displayNameJa = null,
            string? displayNameEn = null)
        {
            TypeName = typeName;
            CommandType = commandType;
            SettingsType = settingsType;
            Category = category;
            IsIfCommand = isIfCommand;
            IsLoopCommand = isLoopCommand;
            IsEndCommand = isEndCommand;
            DisplayPriority = displayPriority;
            DisplaySubPriority = displaySubPriority;
            DisplayNameJa = string.IsNullOrWhiteSpace(displayNameJa) ? typeName : displayNameJa;
            DisplayNameEn = string.IsNullOrWhiteSpace(displayNameEn) ? typeName : displayNameEn;
        }
    }

    /// <summary>
    /// �R�}���h�̃J�e�S��
    /// </summary>
    public enum CommandCategory
    {
        Action,        // ��{�A�N�V����
        Control,       // ����\��
        AI,           // AI�֘A
        System,       // �V�X�e��
        Variable      // �ϐ�
    }
}

