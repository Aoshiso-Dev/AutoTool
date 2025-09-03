using System.Collections.Generic;

namespace AutoTool.Services
{
    /// <summary>
    /// �ŋߊJ�����t�@�C���Ǘ��T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IRecentFileService
    {
        /// <summary>
        /// �ŋߊJ�����t�@�C���ꗗ���擾
        /// </summary>
        IEnumerable<string> GetRecentFiles();

        /// <summary>
        /// �ŋߊJ�����t�@�C����ǉ�
        /// </summary>
        void AddRecentFile(string filePath);

        /// <summary>
        /// �ŋߊJ�����t�@�C�����폜
        /// </summary>
        void RemoveRecentFile(string filePath);

        /// <summary>
        /// �ŋߊJ�����t�@�C�����N���A
        /// </summary>
        void ClearRecentFiles();
    }

    /// <summary>
    /// �ŋߊJ�����t�@�C���Ǘ��T�[�r�X�̎���
    /// </summary>
    public class RecentFileService : IRecentFileService
    {
        private readonly List<string> _recentFiles = new List<string>();

        public IEnumerable<string> GetRecentFiles()
        {
            return _recentFiles.AsReadOnly();
        }

        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            // �����̂��̂��폜
            _recentFiles.Remove(filePath);
            
            // �擪�ɒǉ�
            _recentFiles.Insert(0, filePath);
            
            // �ő�10���܂�
            while (_recentFiles.Count > 10)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }
        }

        public void RemoveRecentFile(string filePath)
        {
            _recentFiles.Remove(filePath);
        }

        public void ClearRecentFiles()
        {
            _recentFiles.Clear();
        }
    }
}