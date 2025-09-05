using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services
{
    /// <summary>
    /// 最近使用したファイルの管理サービスインターフェース
    /// </summary>
    public interface IRecentFileService
    {
        /// <summary>
        /// 最近使用したファイルのリスト
        /// </summary>
        ObservableCollection<RecentFileItem> RecentFiles { get; }

        /// <summary>
        /// ファイルを最近使用したファイルリストに追加
        /// </summary>
        void AddRecentFile(string filePath);

        /// <summary>
        /// 最近使用したファイルリストをクリア
        /// </summary>
        void ClearRecentFiles();

        /// <summary>
        /// 最近使用したファイルリストを読み込み
        /// </summary>
        void LoadRecentFiles();

        /// <summary>
        /// 最近使用したファイルリストを保存
        /// </summary>
        void SaveRecentFiles();
    }

    /// <summary>
    /// 最近使用したファイルのアイテム
    /// </summary>
    public class RecentFileItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 最近使用したファイルの管理サービス実装
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
                    _logger.LogWarning("無効なファイルパス: {FilePath}", filePath);
                    return;
                }

                var fileName = System.IO.Path.GetFileName(filePath);
                var existingItem = _recentFiles.FirstOrDefault(x => x.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    // 既存のアイテムを更新して先頭に移動
                    _recentFiles.Remove(existingItem);
                    existingItem.LastAccessed = DateTime.Now;
                    _recentFiles.Insert(0, existingItem);
                }
                else
                {
                    // 新しいアイテムを先頭に追加
                    var newItem = new RecentFileItem
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        LastAccessed = DateTime.Now
                    };
                    _recentFiles.Insert(0, newItem);
                }

                // 最大数を超えた場合は古いものを削除
                while (_recentFiles.Count > MaxRecentFiles)
                {
                    _recentFiles.RemoveAt(_recentFiles.Count - 1);
                }

                SaveRecentFiles();
                _logger.LogDebug("最近使用したファイルを追加: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用したファイル追加中にエラー: {FilePath}", filePath);
            }
        }

        public void ClearRecentFiles()
        {
            try
            {
                _recentFiles.Clear();
                SaveRecentFiles();
                _logger.LogInformation("最近使用したファイルリストをクリアしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用したファイルリストクリア中にエラー");
            }
        }

        public void LoadRecentFiles()
        {
            try
            {
                // 簡易実装：今回はメモリ内のみで管理
                _logger.LogDebug("最近使用したファイルリストを読み込みました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用したファイルリスト読み込み中にエラー");
            }
        }

        public void SaveRecentFiles()
        {
            try
            {
                // 簡易実装：今回はメモリ内のみで管理
                _logger.LogDebug("最近使用したファイルリストを保存しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近使用したファイルリスト保存中にエラー");
            }
        }
    }
}