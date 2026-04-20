namespace AutoTool.Application.Ports;

/// <summary>
/// ファイルシステムの基本パス操作を抽象化するポートです。
/// </summary>
public interface IFileSystemPathService
{
    /// <summary>ファイルの存在有無を返します。</summary>
    bool FileExists(string filePath);
    /// <summary>パスからファイル名を取得します。</summary>
    string GetFileName(string filePath);
}
