using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroPanels.ViewModel.Shared
{
    /// <summary>
    /// Undo/Redo�R�}���h�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IUndoRedoCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }

    /// <summary>
    /// Undo/Redo�@�\�̗����Ǘ�
    /// </summary>
    public class CommandHistoryManager
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();
        private const int MaxHistoryCount = 50; // �����̍ő�ێ���

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : string.Empty;
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : string.Empty;

        public event EventHandler? HistoryChanged;

        /// <summary>
        /// �V�����R�}���h�����s���ė����ɒǉ�
        /// </summary>
        public void ExecuteCommand(IUndoRedoCommand command)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] ExecuteCommand�J�n: {command.Description}");

            command.Execute();
            _undoStack.Push(command);

            // Redo�X�^�b�N���N���A�i�V�����R�}���h�����s���ꂽ���߁j
            _redoStack.Clear();

            // �����̐������`�F�b�N
            while (_undoStack.Count > MaxHistoryCount)
            {
                var oldCommands = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 1; i < oldCommands.Length; i++)
                {
                    _undoStack.Push(oldCommands[oldCommands.Length - 1 - i]);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] ExecuteCommand����: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// Undo���������s
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                System.Diagnostics.Debug.WriteLine("[CommandHistory] Undo���s: UndoStack����ł�");
                return;
            }

            var command = _undoStack.Pop();
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Undo���s: {command.Description}");

            command.Undo();
            _redoStack.Push(command);

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Undo����: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// Redo���������s
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
            {
                System.Diagnostics.Debug.WriteLine("[CommandHistory] Redo���s: RedoStack����ł�");
                return;
            }

            var command = _redoStack.Pop();
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Redo���s: {command.Description}");

            command.Execute();
            _undoStack.Push(command);

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Redo����: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// �������N���A
        /// </summary>
        public void Clear()
        {
            var undoCount = _undoStack.Count;
            var redoCount = _redoStack.Count;

            _undoStack.Clear();
            _redoStack.Clear();

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Clear���s: �폜���ꂽUndo={undoCount}, Redo={redoCount}");
            OnHistoryChanged();
        }

        /// <summary>
        /// �f�o�b�O�p�F�����̏ڍׂ��擾
        /// </summary>
        public (string[] UndoHistory, string[] RedoHistory) GetHistoryDetails()
        {
            var undoHistory = _undoStack.Select(cmd => cmd.Description).ToArray();
            var redoHistory = _redoStack.Select(cmd => cmd.Description).ToArray();
            return (undoHistory, redoHistory);
        }

        private void OnHistoryChanged()
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}