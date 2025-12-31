namespace MacroPanels.Command.Services;

/// <summary>
/// パス操作サービスのインターフェース
/// </summary>
public interface IPathService
{
    /// <summary>
    /// 相対パスを絶対パスに変換します
    /// </summary>
    /// <param name="relativePath">相対パス</param>
    /// <returns>絶対パス</returns>
    string ToAbsolutePath(string relativePath);

    /// <summary>
    /// 絶対パスを相対パスに変換します
    /// </summary>
    /// <param name="absolutePath">絶対パス</param>
    /// <returns>相対パス</returns>
    string ToRelativePath(string absolutePath);

    /// <summary>
    /// 基準ディレクトリを取得します
    /// </summary>
    string BaseDirectory { get; }
}
