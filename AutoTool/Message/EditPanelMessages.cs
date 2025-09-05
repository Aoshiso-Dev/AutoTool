using AutoTool.Model.CommandDefinition;
using System;

namespace AutoTool.Message
{
    /// <summary>
    /// EditPanelのアイテム更新メッセージ
    /// </summary>
    [Obsolete("標準MVVM方式に移行。ChangeSelectedMessageを使用してください。", false)]
    public class UpdateEditPanelItemMessage
    {
        public UniversalCommandItem? Item { get; }

        public UpdateEditPanelItemMessage(UniversalCommandItem? item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// EditPanelのプロパティ設定メッセージ
    /// </summary>
    [Obsolete("標準MVVM方式に移行。直接プロパティを設定してください。", false)]
    public class SetEditPanelPropertyMessage
    {
        public string PropertyName { get; }
        public object? Value { get; }

        public SetEditPanelPropertyMessage(string propertyName, object? value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }

    /// <summary>
    /// EditPanelのプロパティ要求メッセージ
    /// </summary>
    [Obsolete("標準MVVM方式に移行。直接プロパティを参照してください。", false)]
    public class RequestEditPanelPropertyMessage
    {
        public string PropertyName { get; }

        public RequestEditPanelPropertyMessage(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// EditPanelのプロパティ応答メッセージ
    /// </summary>
    [Obsolete("標準MVVM方式に移行。直接プロパティを参照してください。", false)]
    public class EditPanelPropertyResponseMessage
    {
        public string PropertyName { get; }
        public object? Value { get; }

        public EditPanelPropertyResponseMessage(string propertyName, object? value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }

    /// <summary>
    /// EditPanelの実行状態更新メッセージ
    /// </summary>
    [Obsolete("標準MVVM方式に移行。直接プロパティを設定してください。", false)]
    public class UpdateEditPanelRunningStateMessage
    {
        public bool IsRunning { get; }

        public UpdateEditPanelRunningStateMessage(bool isRunning)
        {
            IsRunning = isRunning;
        }
    }
}