namespace AutoTool.Services.Abstractions;

/// <summary>
/// 最近使用したファイルサービスのインターフェース
/// </summary>
public interface IRecentFileService
{
    /// <summary>
    /// 最近使用したファイルの一覧を取得します
    /// </summary>
    IEnumerable<string> GetRecentFiles();
    
    /// <summary>
    /// 最近使用したファイルの一覧プロパティ
    /// </summary>
    IEnumerable<string> RecentFiles { get; }
    
    /// <summary>
    /// ファイルを最近使用したファイルリストに追加します
    /// </summary>
    void AddRecentFile(string filePath);
    
    /// <summary>
    /// 最近使用したファイルリストからファイルを削除します
    /// </summary>
    void RemoveRecentFile(string filePath);
    
    /// <summary>
    /// 最近使用したファイルリストをクリアします
    /// </summary>
    void ClearRecentFiles();
    
    /// <summary>
    /// 最近使用したファイルリストを保存します
    /// </summary>
    void SaveRecentFiles();
    
    /// <summary>
    /// 最大保持ファイル数を取得または設定します
    /// </summary>
    int MaxRecentFiles { get; set; }
}