using System;
using System.Collections.Generic;
using System.Linq;
using MacroPanels.Model.List.Interface;

namespace AutoTool.Model
{
    /// <summary>
    /// Undo/Redo�̑�����Ǘ�����R�}���h�C���^�[�t�F�[�X
    /// </summary>
    public interface IUndoRedoCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }

    /// <summary>
    /// �R�}���h���X�g�A�C�e���̒ǉ�����
    /// </summary>
    public class AddItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _addAction;
        private readonly Action<int> _removeAction;
        private readonly ICommandListItem _item;
        private readonly int _index;

        public string Description => $"�A�C�e���ǉ�: {_item.ItemType} (�s{_index + 1})";

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
    /// �R�}���h���X�g�A�C�e���̍폜����
    /// </summary>
    public class RemoveItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _addAction;
        private readonly Action<int> _removeAction;
        private readonly ICommandListItem _item;
        private readonly int _index;

        public string Description => $"�A�C�e���폜: {_item.ItemType} (�s{_index + 1})";

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
    /// �R�}���h���X�g�A�C�e���̈ړ�����
    /// </summary>
    public class MoveItemCommand : IUndoRedoCommand
    {
        private readonly Action<int, int> _moveAction;
        private readonly int _fromIndex;
        private readonly int _toIndex;

        public string Description => $"�A�C�e���ړ�: �s{_fromIndex + 1} �� �s{_toIndex + 1}";

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
    /// �R�}���h���X�g�A�C�e���̕ҏW����
    /// </summary>
    public class EditItemCommand : IUndoRedoCommand
    {
        private readonly Action<ICommandListItem, int> _replaceAction;
        private readonly ICommandListItem _oldItem;
        private readonly ICommandListItem _newItem;
        private readonly int _index;

        public string Description => $"�A�C�e���ҏW: {_oldItem.ItemType} (�s{_index + 1})";

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
    /// �S�N���A����
    /// </summary>
    public class ClearAllCommand : IUndoRedoCommand
    {
        private readonly Action _clearAction;
        private readonly Action<IEnumerable<ICommandListItem>> _restoreAction;
        private readonly List<ICommandListItem> _savedItems;

        public string Description => $"�S�N���A ({_savedItems.Count}�A�C�e��)";

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
    /// Undo/Redo����̗����Ǘ�
    /// </summary>
    public class CommandHistoryManager
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();
        private const int MaxHistoryCount = 50; // �����̍ő�ێ���

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "�Ȃ�";
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "�Ȃ�";

        public event EventHandler? HistoryChanged;

        /// <summary>
        /// �V�����R�}���h�����s���ė����ɒǉ�
        /// </summary>
        public void ExecuteCommand(IUndoRedoCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // �V�����R�}���h���s��Redo�X�^�b�N���N���A

            // �����̏���Ǘ�
            while (_undoStack.Count > MaxHistoryCount)
            {
                var oldCommands = _undoStack.ToArray().Reverse().ToArray();
                _undoStack.Clear();
                for (int i = 1; i < oldCommands.Length; i++) // �ŌÂ�1�������čĒǉ�
                {
                    _undoStack.Push(oldCommands[i]);
                }
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undo��������s
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
        /// Redo��������s
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
        /// �������N���A
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// �f�o�b�O�p�F�����̏ڍׂ��擾
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