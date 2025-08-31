using System;
using System.IO;
using System.Reflection;

namespace MacroPanels.Helpers
{
    /// <summary>
    /// ファイルパスの相対パス・絶対パス変換を行うヘルパークラス
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// AutoTool.exeがあるディレクトリのパスを取得
        /// </summary>
        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// 絶対パスを相対パスに変換する
        /// 相対パスに変換できない場合は絶対パスを返す
        /// </summary>
        /// <param name="absolutePath">絶対パス</param>
        /// <returns>相対パスまたは絶対パス</returns>
        public static string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            try
            {
                var appDirectory = GetApplicationDirectory();
                var uri1 = new Uri(appDirectory + Path.DirectorySeparatorChar);
                var uri2 = new Uri(absolutePath);
                
                if (uri1.Scheme != uri2.Scheme)
                {
                    // スキームが異なる場合（例：ネットワークパス）は絶対パスを返す
                    return absolutePath;
                }

                var relativeUri = uri1.MakeRelativeUri(uri2);
                var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                
                // パス区切り文字を正規化
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                // 変換に失敗した場合は絶対パスを返す
                System.Diagnostics.Debug.WriteLine($"相対パス変換に失敗: {ex.Message}");
                return absolutePath;
            }
        }

        /// <summary>
        /// 相対パスを絶対パスに変換する
        /// 既に絶対パスの場合はそのまま返す
        /// </summary>
        /// <param name="relativePath">相対パスまたは絶対パス</param>
        /// <returns>絶対パス</returns>
        public static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return relativePath;

            try
            {
                // 既に絶対パスの場合はそのまま返す
                if (Path.IsPathRooted(relativePath))
                    return relativePath;

                var appDirectory = GetApplicationDirectory();
                var absolutePath = Path.Combine(appDirectory, relativePath);
                
                // パスを正規化
                return Path.GetFullPath(absolutePath);
            }
            catch (Exception ex)
            {
                // 変換に失敗した場合は元のパスを返す
                System.Diagnostics.Debug.WriteLine($"絶対パス変換に失敗: {ex.Message}");
                return relativePath;
            }
        }

        /// <summary>
        /// ファイルが存在するかチェック（相対パス対応）
        /// </summary>
        /// <param name="filePath">ファイルパス（相対または絶対）</param>
        /// <returns>ファイルが存在するかどうか</returns>
        public static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                var absolutePath = ToAbsolutePath(filePath);
                return File.Exists(absolutePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ディレクトリが存在するかチェック（相対パス対応）
        /// </summary>
        /// <param name="directoryPath">ディレクトリパス（相対または絶対）</param>
        /// <returns>ディレクトリが存在するかどうか</returns>
        public static bool DirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            try
            {
                var absolutePath = ToAbsolutePath(directoryPath);
                return Directory.Exists(absolutePath);
            }
            catch
            {
                return false;
            }
        }
    }
}