using System.Collections.Generic;

namespace AutoTool.Services
{
    /// <summary>
    /// 最近開いたファイル管理サービスのインターフェース
    /// </summary>
    public interface IRecentFileService
    {
        /// <summary>
        /// 最近開いたファイル一覧を取得
        /// </summary>
        IEnumerable<string> GetRecentFiles();

        /// <summary>
        /// 最近開いたファイルを追加
        /// </summary>
        void AddRecentFile(string filePath);

        /// <summary>
        /// 最近開いたファイルを削除
        /// </summary>
        void RemoveRecentFile(string filePath);

        /// <summary>
        /// 最近開いたファイルをクリア
        /// </summary>
        void ClearRecentFiles();
    }

    /// <summary>
    /// 最近開いたファイル管理サービスの実装
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

            // 既存のものを削除
            _recentFiles.Remove(filePath);
            
            // 先頭に追加
            _recentFiles.Insert(0, filePath);
            
            // 最大10件まで
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