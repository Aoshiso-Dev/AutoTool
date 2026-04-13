using System.Collections.Generic;
using System.Linq;

namespace AutoTool.Model;

public class CommandHistoryManager
{
    private readonly Stack<IUndoRedoCommand> _undoStack = new();
    private readonly Stack<IUndoRedoCommand> _redoStack = new();
    private const int MaxHistoryCount = 50;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "‚È‚µ";
    public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "‚È‚µ";

    public event EventHandler? HistoryChanged;

    public void ExecuteCommand(IUndoRedoCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }
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

    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public (string[] UndoHistory, string[] RedoHistory) GetHistoryDetails()
    {
        return (
            _undoStack.Select(cmd => cmd.Description).ToArray(),
            _redoStack.Select(cmd => cmd.Description).ToArray()
        );
    }
}
