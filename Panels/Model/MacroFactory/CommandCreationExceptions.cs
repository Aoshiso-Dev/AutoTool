using System;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// コマンド生成時の例外基底クラス
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
            var location = LineNumber.HasValue ? $" (行 {LineNumber})" : "";
            var type = !string.IsNullOrEmpty(ItemType) ? $" [{ItemType}]" : "";
            return $"{GetType().Name}{type}{location}: {Message}";
        }
    }

    /// <summary>
    /// ペア関係が正しくない場合の例外
    /// </summary>
    public class PairMismatchException : CommandCreationException
    {
        public PairMismatchException(string message, int lineNumber, string itemType)
            : base(message, lineNumber, itemType) { }
    }

    /// <summary>
    /// 空の構造体（IfやLoop内に要素がない）の例外
    /// </summary>
    public class EmptyStructureException : CommandCreationException
    {
        public EmptyStructureException(string message, int lineNumber, string itemType)
            : base(message, lineNumber, itemType) { }
    }

    /// <summary>
    /// 未対応のコマンド型の例外
    /// </summary>
    public class UnsupportedCommandTypeException : CommandCreationException
    {
        public UnsupportedCommandTypeException(string message, int? lineNumber = null, string? itemType = null)
            : base(message, lineNumber, itemType) { }
    }
}