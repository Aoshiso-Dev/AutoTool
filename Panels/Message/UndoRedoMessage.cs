using System;

namespace MacroPanels.Message
{
    /// <summary>
    /// ���ɖ߂������v�����郁�b�Z�[�W
    /// </summary>
    public class UndoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    /// <summary>
    /// ��蒼�������v�����郁�b�Z�[�W
    /// </summary>
    public class RedoMessage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}