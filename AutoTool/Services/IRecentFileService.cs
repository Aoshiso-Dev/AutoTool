using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services
{
    /// <summary>
    /// �ŋߎg�p�����t�@�C���̊Ǘ��T�[�r�X�C���^�[�t�F�[�X
    /// </summary>
    public interface IRecentFileService
    {
        /// <summary>
        /// �ŋߎg�p�����t�@�C���̃��X�g
        /// </summary>
        ObservableCollection<RecentFileItem> RecentFiles { get; }

        /// <summary>
        /// �t�@�C�����ŋߎg�p�����t�@�C�����X�g�ɒǉ�
        /// </summary>
        void AddRecentFile(string filePath);

        /// <summary>
        /// �ŋߎg�p�����t�@�C�����X�g���N���A
        /// </summary>
        void ClearRecentFiles();

        /// <summary>
        /// �ŋߎg�p�����t�@�C�����X�g��ǂݍ���
        /// </summary>
        void LoadRecentFiles();

        /// <summary>
        /// �ŋߎg�p�����t�@�C�����X�g��ۑ�
        /// </summary>
        void SaveRecentFiles();
    }

    /// <summary>
    /// �ŋߎg�p�����t�@�C���̃A�C�e��
    /// </summary>
    public class RecentFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// �ŋߎg�p�����t�@�C���̊Ǘ��T�[�r�X����
    /// </summary>
    public class RecentFileService : IRecentFileService
    {
        private readonly ILogger<RecentFileService> _logger;
        private readonly ObservableCollection<RecentFileItem> _recentFiles = new();
        private const int MaxRecentFiles = 10;

        public ObservableCollection<RecentFileItem> RecentFiles => _recentFiles;

        public RecentFileService(ILogger<RecentFileService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadRecentFiles();
        }

        public void AddRecentFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("�����ȃt�@�C���p�X: {FilePath}", filePath);
                    return;
                }

                var fileName = System.IO.Path.GetFileName(filePath);
                var existingItem = _recentFiles.FirstOrDefault(x => x.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    // �����̃A�C�e�����X�V���Đ擪�Ɉړ�
                    _recentFiles.Remove(existingItem);
                    existingItem.LastAccessed = DateTime.Now;
                    _recentFiles.Insert(0, existingItem);
                }
                else
                {
                    // �V�����A�C�e����擪�ɒǉ�
                    var newItem = new RecentFileItem
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        LastAccessed = DateTime.Now
                    };
                    _recentFiles.Insert(0, newItem);
                }

                // �ő吔�𒴂����ꍇ�͌Â����̂��폜
                while (_recentFiles.Count > MaxRecentFiles)
                {
                    _recentFiles.RemoveAt(_recentFiles.Count - 1);
                }

                SaveRecentFiles();
                _logger.LogDebug("�ŋߎg�p�����t�@�C����ǉ�: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�����t�@�C���ǉ����ɃG���[: {FilePath}", filePath);
            }
        }

        public void ClearRecentFiles()
        {
            try
            {
                _recentFiles.Clear();
                SaveRecentFiles();
                _logger.LogInformation("�ŋߎg�p�����t�@�C�����X�g���N���A���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�����t�@�C�����X�g�N���A���ɃG���[");
            }
        }

        public void LoadRecentFiles()
        {
            try
            {
                // �ȈՎ����F����̓��������݂̂ŊǗ�
                _logger.LogDebug("�ŋߎg�p�����t�@�C�����X�g��ǂݍ��݂܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�����t�@�C�����X�g�ǂݍ��ݒ��ɃG���[");
            }
        }

        public void SaveRecentFiles()
        {
            try
            {
                // �ȈՎ����F����̓��������݂̂ŊǗ�
                _logger.LogDebug("�ŋߎg�p�����t�@�C�����X�g��ۑ����܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�����t�@�C�����X�g�ۑ����ɃG���[");
            }
        }
    }
}