using System;
using System.Collections.Generic;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// �R�}���h���W�X�g���̃C���^�[�t�F�[�X
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// ���ׂẴR�}���h�^�C�v�����擾
        /// </summary>
        IEnumerable<string> GetAllTypeNames();

        /// <summary>
        /// UI�\���p�ɏ����t����ꂽ�R�}���h�^�C�v�����擾
        /// </summary>
        IEnumerable<string> GetOrderedTypeNames();

        /// <summary>
        /// �J�e�S���ʂ̃R�}���h�^�C�v�����擾
        /// </summary>
        IEnumerable<string> GetTypeNamesByCategory(CommandCategory category);

        /// <summary>
        /// �\���D��x�ʂ̃R�}���h�^�C�v�����擾
        /// </summary>
        IEnumerable<string> GetTypeNamesByDisplayPriority(int priority);

        /// <summary>
        /// �R�}���h�A�C�e�����쐬
        /// </summary>
        ICommandListItem? CreateCommandItem(string typeName);

        /// <summary>
        /// If�R�}���h���ǂ�������
        /// </summary>
        bool IsIfCommand(string typeName);

        /// <summary>
        /// ���[�v�R�}���h���ǂ�������
        /// </summary>
        bool IsLoopCommand(string typeName);

        /// <summary>
        /// �I���R�}���h�i�l�X�g���x�������炷�j���ǂ�������
        /// </summary>
        bool IsEndCommand(string typeName);

        /// <summary>
        /// �J�n�R�}���h�i�l�X�g���x���𑝂₷�j���ǂ�������
        /// </summary>
        bool IsStartCommand(string typeName);

        /// <summary>
        /// �w�肳�ꂽ�^�C�v���̃A�C�e���^�C�v���擾
        /// </summary>
        Type? GetItemType(string typeName);

        /// <summary>
        /// UI�\���p�̃R�}���h��`�ꗗ���擾
        /// </summary>
        IEnumerable<CommandDefinitionItem> GetCommandDefinitions();

        /// <summary>
        /// ������
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// UI�\���p�̃R�}���h��`����
    /// </summary>
    public class CommandDefinitionItem
    {
        /// <summary>
        /// �^�C�v���i�������ʎq�j
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// �\�����i���{��j
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// �J�e�S��
        /// </summary>
        public CommandCategory Category { get; set; }

        /// <summary>
        /// ������
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// �\���D��x
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// �T�u�D��x
        /// </summary>
        public int SubPriority { get; set; }

        /// <summary>
        /// If�R�}���h�t���O
        /// </summary>
        public bool IsIfCommand { get; set; }

        /// <summary>
        /// ���[�v�R�}���h�t���O
        /// </summary>
        public bool IsLoopCommand { get; set; }
    }
}