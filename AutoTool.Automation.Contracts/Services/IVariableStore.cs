namespace AutoTool.Commands.Services;

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

/// <summary>
/// 変数ストアの変更を画面や診断機能へ通知できる拡張契約です。
/// </summary>
public interface IObservableVariableStore : IVariableStore
{
    /// <summary>
    /// 変数が設定またはクリアされたときに通知します。
    /// </summary>
    event EventHandler<VariableStoreChangedEventArgs>? Changed;

    /// <summary>
    /// 現在保持している変数を一覧で取得します。
    /// </summary>
    IReadOnlyDictionary<string, string> GetSnapshot();
}

/// <summary>
/// 変数ストアの変更内容を表します。
/// </summary>
public sealed class VariableStoreChangedEventArgs : EventArgs
{
    public VariableStoreChangedEventArgs(string? name, string? value, bool isClear)
    {
        Name = name;
        Value = value;
        IsClear = isClear;
    }

    public string? Name { get; }
    public string? Value { get; }
    public bool IsClear { get; }
}
