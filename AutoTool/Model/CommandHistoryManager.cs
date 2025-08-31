using System;
using System.Collections.Generic;
using System.Linq;
using MacroPanels.Model.List.Interface;

namespace AutoTool.Model
{
    /// <summary>
    /// Undo/Redoの操作を管理するコマンドインターフェース
    /// </summary>
    public interface IUndoRedoCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }

    /// <summary>
    /// コマンドリストアイテムの追加操作
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _addAction;
        private readonly Action<int> _removeAction;
        private readonly ICommandListItem _item;
        private readonly int _index;

        public string Description => $"アイテム追加: {_item.ItemType} (行{_index + 1})";

        public AddItemCommand(ICommandListItem item, int index, 
                             Action<ICommandListItem, int> addAction, 
                             Action<int> removeAction)
        {
            _item = item;
            _index = index;
            _addAction = addAction;
            _removeAction = removeAction;
        }

        public void Execute()
        {
            _addAction(_item, _index);
        }

        public void Undo()
        {
            _removeAction(_index);
        }
    }

    /// <summary>
    /// コマンドリストアイテムの削除操作
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _addAction;
        private readonly Action<int> _removeAction;
        private readonly ICommandListItem _item;
        private readonly int _index;

        public string Description => $"アイテム削除: {_item.ItemType} (行{_index + 1})";

        public RemoveItemCommand(ICommandListItem item, int index,
                                Action<ICommandListItem, int> addAction,
                                Action<int> removeAction)
        {
            _item = item;
            _index = index;
            _addAction = addAction;
            _removeAction = removeAction;
        }

        public void Execute()
        {
            _removeAction(_index);
        }

        public void Undo()
        {
            _addAction(_item, _index);
        }
    }

    /// <summary>
    /// コマンドリストアイテムの移動操作
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly Action<int, int> _moveAction;
        private readonly int _fromIndex;
        private readonly int _toIndex;

        public string Description => $"アイテム移動: 行{_fromIndex + 1} → 行{_toIndex + 1}";

        public MoveItemCommand(int fromIndex, int toIndex, Action<int, int> moveAction)
        {
            _fromIndex = fromIndex;
            _toIndex = toIndex;
            _moveAction = moveAction;
        }

        public void Execute()
        {
            _moveAction(_fromIndex, _toIndex);
        }

        public void Undo()
        {
            _moveAction(_toIndex, _fromIndex);
        }
    }

    /// <summary>
    /// コマンドリストアイテムの編集操作
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _replaceAction;
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;

        public string Description => $"アイテム編集: {_oldItem.ItemType} (行{_index + 1})";

        public EditItemCommand(ICommandListItem oldItem, ICommandListItem newItem, int index,
                              Action<ICommandListItem, int> replaceAction)
        {
            _oldItem = oldItem.Clone();
            _newItem = newItem.Clone();
            _index = index;
            _replaceAction = replaceAction;
        }

        public void Execute()
        {
            _replaceAction(_newItem, _index);
        }

        public void Undo()
        {
            _replaceAction(_oldItem, _index);
        }
    }

    /// <summary>
    /// 全クリア操作
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly Action _clearAction;
        private readonly Action<IEnumerable<ICommandListItem>> _restoreAction;
        private readonly List<ICommandListItem> _savedItems;

        public string Description => $"全クリア ({_savedItems.Count}アイテム)";

        public ClearAllCommand(IEnumerable<ICommandListItem> items,
                              Action clearAction,
                              Action<IEnumerable<ICommandListItem>> restoreAction)
        {
            _savedItems = items.Select(item => item.Clone()).ToList();
            _clearAction = clearAction;
            _restoreAction = restoreAction;
        }

        public void Execute()
        {
            _clearAction();
        }

        public void Undo()
        {
            _restoreAction(_savedItems);
        }
    }

    /// <summary>
    /// Undo/Redo操作の履歴管理
    /// </summary>
    public class CommandHistoryManager
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();
        private const int MaxHistoryCount = 50; // 履歴の最大保持数

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "なし";
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "なし";

        public event EventHandler? HistoryChanged;

        /// <summary>
        /// 新しいコマンドを実行して履歴に追加
        /// </summary>
        public void ExecuteCommand(IUndoRedoCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 新しいコマンド実行でRedoスタックをクリア

            // 履歴の上限管理
            while (_undoStack.Count > MaxHistoryCount)
            {
                var oldCommands = _undoStack.ToArray().Reverse().ToArray();
                _undoStack.Clear();
                for (int i = 1; i < oldCommands.Length; i++) // 最古の1つを除いて再追加
                {
                    _undoStack.Push(oldCommands[i]);
                }
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undo操作を実行
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
        /// Redo操作を実行
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
        /// 履歴をクリア
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// デバッグ用：履歴の詳細を取得
        /// </summary>
        public (string[] UndoHistory, string[] RedoHistory) GetHistoryDetails()
        {
            return (
                _undoStack.Select(cmd => cmd.Description).ToArray(),
                _redoStack.Select(cmd => cmd.Description).ToArray()
            );
        }
    }
}