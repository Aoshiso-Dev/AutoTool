using System.IO;

namespace AutoTool.Application.Files;

/// <summary>
/// 旧形式ファイルの削除を安全に実行する共通ヘルパーです。
/// </summary>
public static class LegacyFileCleanup
{
    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // クリーンアップに失敗しても旧ファイルは保持し、処理は継続します。
        }
    }
}
