namespace AutoTool.Application.History;

/// <summary>
/// 履歴操作で実行・取り消しできるコマンドの共通契約です。
/// </summary>
public interface IUndoRedoCommand
{
    /// <summary>履歴表示用の説明文です。</summary>
    string Description { get; }
    /// <summary>コマンド処理を実行します。</summary>
    void Execute();
    /// <summary>実行結果を取り消します。</summary>
    void Undo();
}
