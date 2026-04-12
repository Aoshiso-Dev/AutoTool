using System;

namespace AutoTool.Panels.Model.MacroFactory
{
    /// <summary>
    /// �R�}���h�������̗�O���N���X
    /// </summary>
    public abstract class CommandCreationException : Exception
    {
        public int? LineNumber { get; }
        public string? ItemType { get; }

        protected CommandCreationException(string message, int? lineNumber = null, string? itemType = null) 
            : base(message)
        {
            LineNumber = lineNumber;
            ItemType = itemType;
        }

        protected CommandCreationException(string message, Exception innerException, int? lineNumber = null, string? itemType = null) 
            : base(message, innerException)
        {
            LineNumber = lineNumber;
            ItemType = itemType;
        }

        public override string ToString()
        {
            var location = LineNumber.HasValue ? $" (�s {LineNumber})" : "";
            var type = !string.IsNullOrEmpty(ItemType) ? $" [{ItemType}]" : "";
            return $"{GetType().Name}{type}{location}: {Message}";
        }
    }

    /// <summary>
    /// �y�A�֌W���������Ȃ��ꍇ�̗�O
    /// </summary>
    public class PairMismatchException : CommandCreationException
    {
        public PairMismatchException(string message, int lineNumber, string itemType)
            : base(message, lineNumber, itemType) { }
    }

    /// <summary>
    /// ��̍\���́iIf��Loop��ɗv�f���Ȃ��j�̗�O
    /// </summary>
    public class EmptyStructureException : CommandCreationException
    {
        public EmptyStructureException(string message, int lineNumber, string itemType)
            : base(message, lineNumber, itemType) { }
    }

    /// <summary>
    /// ���Ή��̃R�}���h�^�̗�O
    /// </summary>
    public class UnsupportedCommandTypeException : CommandCreationException
    {
        public UnsupportedCommandTypeException(string message, int? lineNumber = null, string? itemType = null)
            : base(message, lineNumber, itemType) { }
    }
}

