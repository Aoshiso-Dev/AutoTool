using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Model.List.Interface;

namespace AutoTool.ViewModel.Shared.UndoRedoCommands
{
    /// <summary>
    /// Phase 5完全統合版：UndoRedoコマンド実装
    /// </summary>
    
    /// <summary>
    /// アイテム追加コマンド
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _executeAction;
        private readonly Action<int> _undoAction;

        public string Description => $"アイテム追加: {_item.ItemType} (位置: {_index})";

        public AddItemCommand(
            ICommandListItem item, 
            int index, 
            Action<ICommandListItem, int> executeAction, 
            Action<int> undoAction)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _index = index;
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
        }

        public void Execute()
        {
            _executeAction(_item, _index);
        }

        public void Undo()
        {
            _undoAction(_index);
        }
    }

    /// <summary>
    /// アイテム削除コマンド
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _undoAction;
        private readonly Action<int> _executeAction;

        public string Description => $"アイテム削除: {_item.ItemType} (位置: {_index})";

        public RemoveItemCommand(
            ICommandListItem item, 
            int index, 
            Action<ICommandListItem, int> undoAction, 
            Action<int> executeAction)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _index = index;
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        }

        public void Execute()
        {
            _executeAction(_index);
        }

        public void Undo()
        {
            _undoAction(_item, _index);
        }
    }

    /// <summary>
    /// アイテム移動コマンド
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly int _fromIndex;
        private readonly int _toIndex;
        private readonly Action<int, int> _moveAction;

        public string Description => $"アイテム移動: {_fromIndex} → {_toIndex}";

        public MoveItemCommand(int fromIndex, int toIndex, Action<int, int> moveAction)
        {
            _fromIndex = fromIndex;
            _toIndex = toIndex;
            _moveAction = moveAction ?? throw new ArgumentNullException(nameof(moveAction));
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
    /// アイテム編集コマンド
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _replaceAction;

        public string Description => $"アイテム編集: {_newItem.ItemType} (位置: {_index})";

        public EditItemCommand(
            ICommandListItem oldItem, 
            ICommandListItem newItem, 
            int index, 
            Action<ICommandListItem, int> replaceAction)
        {
            _oldItem = oldItem ?? throw new ArgumentNullException(nameof(oldItem));
            _newItem = newItem ?? throw new ArgumentNullException(nameof(newItem));
            _index = index;
            _replaceAction = replaceAction ?? throw new ArgumentNullException(nameof(replaceAction));
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
    /// 全クリアコマンド
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly IEnumerable<ICommandListItem> _items;
        private readonly Action _clearAction;
        private readonly Action<IEnumerable<ICommandListItem>> _restoreAction;

        public string Description => $"全クリア: {_items.Count()}件";

        public ClearAllCommand(
            IEnumerable<ICommandListItem> items, 
            Action clearAction, 
            Action<IEnumerable<ICommandListItem>> restoreAction)
        {
            _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
            _clearAction = clearAction ?? throw new ArgumentNullException(nameof(clearAction));
            _restoreAction = restoreAction ?? throw new ArgumentNullException(nameof(restoreAction));
        }

        public void Execute()
        {
            _clearAction();
        }

        public void Undo()
        {
            _restoreAction(_items);
        }
    }
}