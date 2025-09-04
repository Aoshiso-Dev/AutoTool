using System.Collections.Generic;

namespace AutoTool.Services
{
    /// <summary>
    /// �ϐ��X�g�A�̃C���^�[�t�F�[�X�iAutoTool�Łj
    /// </summary>
    public interface IVariableStore
    {
        /// <summary>
        /// �ϐ���ݒ�
        /// </summary>
        void Set(string name, string value);

        /// <summary>
        /// �ϐ����擾
        /// </summary>
        string? Get(string name);

        /// <summary>
        /// �S�Ă̕ϐ����N���A
        /// </summary>
        void Clear();

        /// <summary>
        /// �S�Ă̕ϐ����擾
        /// </summary>
        Dictionary<string, string> GetAll();

        /// <summary>
        /// �ϐ������݂��邩�`�F�b�N
        /// </summary>
        bool Contains(string name);

        /// <summary>
        /// �ϐ����폜
        /// </summary>
        bool Remove(string name);

        /// <summary>
        /// �ϐ��̐����擾
        /// </summary>
        int Count { get; }
    }
}