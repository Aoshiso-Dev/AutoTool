using System;

namespace MacroPanels.Message
{
    /// <summary>
    /// 元に戻す操作を要求するメッセージ
    /// </summary>
    public class UndoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    /// <summary>
    /// やり直し操作を要求するメッセージ
    /// </summary>
    public class RedoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}