using System;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// Phase 4�����ŁF�R�}���h��`�p�̑����B���̑�����t�����N���X�������I�ɃR�}���h�^�C�v�Ƃ��ēo�^����
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
        /// �R�}���h�J�e�S��
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

        public CommandDefinitionAttribute(
            string typeName, 
            Type commandType, 
            Type settingsType, 
            CommandCategory category = CommandCategory.Action,
            bool isIfCommand = false,
            bool isLoopCommand = false)
        {
            TypeName = typeName;
            CommandType = commandType;
            SettingsType = settingsType;
            Category = category;
            IsIfCommand = isIfCommand;
            IsLoopCommand = isLoopCommand;
        }
    }

    /// <summary>
    /// �R�}���h�̃J�e�S���iPhase 4�����Łj
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