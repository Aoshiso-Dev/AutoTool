namespace AutoTool.Services.Abstractions;

/// <summary>
/// 設定サービスのインターフェース
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 設定値を取得します
    /// </summary>
    T? Get<T>(string key, T? defaultValue = default);
    
    /// <summary>
    /// 設定値を設定します
    /// </summary>
    void Set<T>(string key, T value);
    
    /// <summary>
    /// 設定を保存します
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 設定を読み込みます
    /// </summary>
    Task LoadAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定されたキーの設定が存在するかどうかを確認します
    /// </summary>
    bool ContainsKey(string key);
    
    /// <summary>
    /// 指定されたキーの設定を削除します
    /// </summary>
    void Remove(string key);
    
    /// <summary>
    /// すべての設定をクリアします
    /// </summary>
    void Clear();
    
    /// <summary>
    /// 設定ファイルのパスを取得します
    /// </summary>
    string ConfigFilePath { get; }
}