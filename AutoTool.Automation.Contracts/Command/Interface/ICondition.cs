namespace AutoTool.Commands.Interface;

/// <summary>
/// 条件評価処理の共通契約です。
/// </summary>
public interface ICondition
{
    /// <summary>条件評価設定です。</summary>
    IConditionSettings Settings { get; }

    /// <summary>現在状態で条件が成立するかを評価します。</summary>
    Task<bool> Evaluate(CancellationToken cancellationToken);
}
