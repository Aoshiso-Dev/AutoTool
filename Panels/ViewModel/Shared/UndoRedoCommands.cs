using System;
using System.Collections.Generic;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.ViewModel.Shared
{
    /// <summary>
    /// �A�C�e���ǉ��p��Undo/Redo�R�}���h
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _doAction;
        private readonly Action<int> _undoAction;

        public string Description => $"�A�C�e���ǉ�: {_item.ItemType} (�ʒu: {_index})";

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
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Execute�J�n: {Description}");
            _doAction(_item, _index);
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Execute����: {Description}");
        }

        public void Undo()
        {
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Undo�J�n: {Description}");
            _undoAction(_index);
            System.Diagnostics.Debug.WriteLine($"[AddItemCommand] Undo����: {Description}");
        }
    }

    /// <summary>
    /// �A�C�e���폜�p��Undo/Redo�R�}���h
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _item;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _undoAction;
        private readonly Action<int> _doAction;

        public string Description => $"�A�C�e���폜: {_item.ItemType} (�ʒu: {_index})";

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
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Execute�J�n: {Description}");
            _doAction(_index);
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Execute����: {Description}");
        }

        public void Undo()
        {
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Undo�J�n: {Description}");
            _undoAction(_item, _index);
            System.Diagnostics.Debug.WriteLine($"[RemoveItemCommand] Undo����: {Description}");
        }
    }

    /// <summary>
    /// �S�A�C�e���N���A�p��Undo/Redo�R�}���h
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly List<ICommandListItem> _items;
        private readonly Action _doAction;
        private readonly Action<IEnumerable<ICommandListItem>> _undoAction;

        public string Description => $"�S�N���A: {_items.Count}��";

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
    /// �A�C�e���ړ��p��Undo/Redo�R�}���h
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly int _fromIndex;
        private readonly int _toIndex;
        private readonly Action<int, int> _moveAction;

        public string Description => $"�A�C�e���ړ�: {_fromIndex} �� {_toIndex}";

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
    /// �A�C�e���ҏW�p��Undo/Redo�R�}���h
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;
        private readonly Action<ICommandListItem, int> _replaceAction;

        public string Description => $"�A�C�e���ҏW: {_oldItem.ItemType} (�ʒu: {_index})";

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