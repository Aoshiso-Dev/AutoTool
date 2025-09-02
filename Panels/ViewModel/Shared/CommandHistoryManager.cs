using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroPanels.ViewModel.Shared
{
    /// <summary>
    /// Undo/Redoコマンドのインターフェース
    /// </summary>
    public interface IUndoRedoCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }

    /// <summary>
    /// Undo/Redo機能の履歴管理
    /// </summary>
    public class CommandHistoryManager
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();
        private const int MaxHistoryCount = 50; // 履歴の最大保持数

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : string.Empty;
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : string.Empty;

        public event EventHandler? HistoryChanged;

        /// <summary>
        /// 新しいコマンドを実行して履歴に追加
        /// </summary>
        public void ExecuteCommand(IUndoRedoCommand command)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] ExecuteCommand開始: {command.Description}");

            command.Execute();
            _undoStack.Push(command);

            // Redoスタックをクリア（新しいコマンドが実行されたため）
            _redoStack.Clear();

            // 履歴の数制限チェック
            while (_undoStack.Count > MaxHistoryCount)
            {
                var oldCommands = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 1; i < oldCommands.Length; i++)
                {
                    _undoStack.Push(oldCommands[oldCommands.Length - 1 - i]);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] ExecuteCommand完了: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// Undo処理を実行
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                System.Diagnostics.Debug.WriteLine("[CommandHistory] Undo失敗: UndoStackが空です");
                return;
            }

            var command = _undoStack.Pop();
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Undo実行: {command.Description}");

            command.Undo();
            _redoStack.Push(command);

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Undo完了: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// Redo処理を実行
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
            {
                System.Diagnostics.Debug.WriteLine("[CommandHistory] Redo失敗: RedoStackが空です");
                return;
            }

            var command = _redoStack.Pop();
            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Redo実行: {command.Description}");

            command.Execute();
            _undoStack.Push(command);

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Redo完了: UndoStack={_undoStack.Count}, RedoStack={_redoStack.Count}");
            OnHistoryChanged();
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void Clear()
        {
            var undoCount = _undoStack.Count;
            var redoCount = _redoStack.Count;

            _undoStack.Clear();
            _redoStack.Clear();

            System.Diagnostics.Debug.WriteLine($"[CommandHistory] Clear実行: 削除されたUndo={undoCount}, Redo={redoCount}");
            OnHistoryChanged();
        }

        /// <summary>
        /// デバッグ用：履歴の詳細を取得
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