using System.Collections.Generic;
using System.Linq;

namespace AutoTool.Application.History;

/// <summary>
/// 取り消し/やり直しの履歴スタックを管理し、履歴操作を提供します。
/// </summary>
public class CommandHistoryManager
{
    private readonly Stack<IUndoRedoCommand> _undoStack = new();
    private readonly Stack<IUndoRedoCommand> _redoStack = new();
    private const int MaxHistoryCount = 50;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "なし";
    public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "なし";

    public event EventHandler? HistoryChanged;

    /// <summary>
    /// コマンドを実行して取り消し履歴へ積み、やり直し履歴を破棄します。
    /// </summary>
    public void ExecuteCommand(IUndoRedoCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();

        while (_undoStack.Count > MaxHistoryCount)
        {
            var oldCommands = _undoStack.ToArray().Reverse().ToArray();
            _undoStack.Clear();
            for (var i = 1; i < oldCommands.Length; i++)
            {
                _undoStack.Push(oldCommands[i]);
            }
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 直前のコマンドを取り消し、やり直し履歴へ移動します。
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 取り消したコマンドを再実行し、取り消し履歴へ戻します。
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 取り消し/やり直し履歴をすべてクリアします。
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// デバッグ・表示用に取り消し/やり直しの説明一覧を返します。
    /// </summary>
    public (string[] UndoHistory, string[] RedoHistory) GetHistoryDetails()
    {
        return (
            _undoStack.Select(cmd => cmd.Description).ToArray(),
            _redoStack.Select(cmd => cmd.Description).ToArray()
        );
    }
}

