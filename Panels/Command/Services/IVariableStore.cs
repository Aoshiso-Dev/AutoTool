namespace MacroPanels.Command.Services;

/// <summary>
/// 変数ストアサービスのインターフェース
/// </summary>
public interface IVariableStore
{
    /// <summary>
    /// 変数を設定します
    /// </summary>
    /// <param name="name">変数名</param>
    /// <param name="value">値</param>
    void Set(string name, string value);

    /// <summary>
    /// 変数を取得します
    /// </summary>
    /// <param name="name">変数名</param>
    /// <returns>値（存在しない場合はnull）</returns>
    string? Get(string name);

    /// <summary>
    /// すべての変数をクリアします
    /// </summary>
    void Clear();
}
