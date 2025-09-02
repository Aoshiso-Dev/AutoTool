using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Model.List.Interface;

namespace AutoTool.ViewModel.Shared.UndoRedoCommands
{
    /// <summary>
    /// Phase 5���S�����ŁFUndoRedo�R�}���h����
    /// </summary>
    
    /// <summary>
    /// �A�C�e���ǉ��R�}���h
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _executeAction;
        private readonly Action<int> _undoAction;

        public string Description => $"�A�C�e���ǉ�: {_item.ItemType} (�ʒu: {_index})";

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
    /// �A�C�e���폜�R�}���h
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _undoAction;
        private readonly Action<int> _executeAction;

        public string Description => $"�A�C�e���폜: {_item.ItemType} (�ʒu: {_index})";

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
    /// �A�C�e���ړ��R�}���h
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly int _fromIndex;
        private readonly int _toIndex;
        private readonly Action<int, int> _moveAction;

        public string Description => $"�A�C�e���ړ�: {_fromIndex} �� {_toIndex}";

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
    /// �A�C�e���ҏW�R�}���h
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _replaceAction;

        public string Description => $"�A�C�e���ҏW: {_newItem.ItemType} (�ʒu: {_index})";

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
    /// �S�N���A�R�}���h
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly IEnumerable<ICommandListItem> _items;
        private readonly Action _clearAction;
        private readonly Action<IEnumerable<ICommandListItem>> _restoreAction;

        public string Description => $"�S�N���A: {_items.Count()}��";

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