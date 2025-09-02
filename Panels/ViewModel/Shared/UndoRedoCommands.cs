using System;
using System.Collections.Generic;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.ViewModel.Shared
{
    /// <summary>
    /// アイテム追加用のUndo/Redoコマンド
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _doAction;
        private readonly Action<int> _undoAction;

        public string Description => $"アイテム追加: {_item.ItemType} (位置: {_index})";

        public AddItemCommand(
            ICommandListItem item,
            int index,
            Action<ICommandListItem, int> doAction,
            Action<int> undoAction)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _index = index;
            _doAction = doAction ?? throw new ArgumentNullException(nameof(doAction));
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
        }

        public void Execute()
        {
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Execute開始: {Description}");
            _doAction(_item, _index);
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Execute完了: {Description}");
        }

        public void Undo()
        {
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Undo開始: {Description}");
            _undoAction(_index);
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Undo完了: {Description}");
        }
    }

    /// <summary>
    /// アイテム削除用のUndo/Redoコマンド
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _undoAction;
        private readonly Action<int> _doAction;

        public string Description => $"アイテム削除: {_item.ItemType} (位置: {_index})";

        public RemoveItemCommand(
            ICommandListItem item,
            int index,
            Action<ICommandListItem, int> undoAction,
            Action<int> doAction)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _index = index;
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
            _doAction = doAction ?? throw new ArgumentNullException(nameof(doAction));
        }

        public void Execute()
        {
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Execute開始: {Description}");
            _doAction(_index);
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Execute完了: {Description}");
        }

        public void Undo()
        {
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Undo開始: {Description}");
            _undoAction(_item, _index);
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Undo完了: {Description}");
        }
    }

    /// <summary>
    /// 全アイテムクリア用のUndo/Redoコマンド
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly List<ICommandListItem> _items;
        private readonly Action _doAction;
        private readonly Action<IEnumerable<ICommandListItem>> _undoAction;

        public string Description => $"全クリア: {_items.Count}件";

        public ClearAllCommand(
            List<ICommandListItem> items,
            Action doAction,
            Action<IEnumerable<ICommandListItem>> undoAction)
        {
            _items = new List<ICommandListItem>(items ?? throw new ArgumentNullException(nameof(items)));
            _doAction = doAction ?? throw new ArgumentNullException(nameof(doAction));
            _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
        }

        public void Execute()
        {
            _doAction();
        }

        public void Undo()
        {
            _undoAction(_items);
        }
    }

    /// <summary>
    /// アイテム移動用のUndo/Redoコマンド
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly int _fromIndex;
        private readonly int _toIndex;
        private readonly Action<int, int> _moveAction;

        public string Description => $"アイテム移動: {_fromIndex} → {_toIndex}";

        public MoveItemCommand(
            int fromIndex,
            int toIndex,
            Action<int, int> moveAction)
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
    /// アイテム編集用のUndo/Redoコマンド
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _replaceAction;

        public string Description => $"アイテム編集: {_oldItem.ItemType} (位置: {_index})";

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
}