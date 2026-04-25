using System;
using System.IO;
using System.Reflection;

namespace AutoTool.Infrastructure.Paths;

/// <summary>
/// アプリ基準の相対/絶対パス変換を行うヘルパー。
/// </summary>
public static class ApplicationPathResolver
{
    /// <summary>
    /// 実行ファイルがあるディレクトリを取得する。
    /// </summary>
    public static string GetApplicationDirectory()
    {
        return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// 絶対パスをアプリ基準の相対パスへ変換する。
    /// 変換できない場合は元の絶対パスを返す。
    /// </summary>
    /// <param name="absolutePath">絶対パス</param>
    /// <returns>相対パス、または変換不能時は絶対パス</returns>
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
                // スキームが異なる場合（例: ネットワークパス）はそのまま返す。
                return absolutePath;
            }

            var relativeUri = uri1.MakeRelativeUri(uri2);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            
            // 区切り文字を OS 形式に正規化する。
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            
            return relativePath;
        }
        catch
        {
            // 変換に失敗したら入力値を返す。
            return absolutePath;
        }
    }

    /// <summary>
    /// 相対パスをアプリ基準の絶対パスへ変換する。
    /// すでに絶対パスの場合はそのまま返す。
    /// </summary>
    /// <param name="relativePath">相対パスまたは絶対パス</param>
    /// <returns>絶対パス</returns>
    public static string ToAbsolutePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return relativePath;

        try
        {
            // すでに絶対パスなら変換不要。
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            var appDirectory = GetApplicationDirectory();
            var absolutePath = Path.Combine(appDirectory, relativePath);
            
            // パスを正規化する。
            return Path.GetFullPath(absolutePath);
        }
        catch
        {
            // 変換に失敗したら入力値を返す。
            return relativePath;
        }
    }

    /// <summary>
    /// ファイルが存在するか確認する（相対パス対応）。
    /// </summary>
    /// <param name="filePath">ファイルパス（相対/絶対）</param>
    /// <returns>存在すれば true</returns>
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
    /// ディレクトリが存在するか確認する（相対パス対応）。
    /// </summary>
    /// <param name="directoryPath">ディレクトリパス（相対/絶対）</param>
    /// <returns>存在すれば true</returns>
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

