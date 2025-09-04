using AutoTool.Model.List.Interface;
using System;

namespace AutoTool.Message
{
    /// <summary>
    /// EditPanel�̃A�C�e���X�V���b�Z�[�W
    /// </summary>
    [Obsolete("�W��MVVM�����Ɉڍs�BChangeSelectedMessage���g�p���Ă��������B", false)]
    public class UpdateEditPanelItemMessage
    {
        public ICommandListItem? Item { get; }

        public UpdateEditPanelItemMessage(ICommandListItem? item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// EditPanel�̃v���p�e�B�ݒ胁�b�Z�[�W
    /// </summary>
    [Obsolete("�W��MVVM�����Ɉڍs�B���ڃv���p�e�B��ݒ肵�Ă��������B", false)]
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
    /// EditPanel�̃v���p�e�B�v�����b�Z�[�W
    /// </summary>
    [Obsolete("�W��MVVM�����Ɉڍs�B���ڃv���p�e�B���Q�Ƃ��Ă��������B", false)]
    public class RequestEditPanelPropertyMessage
    {
        public string PropertyName { get; }

        public RequestEditPanelPropertyMessage(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// EditPanel�̃v���p�e�B�������b�Z�[�W
    /// </summary>
    [Obsolete("�W��MVVM�����Ɉڍs�B���ڃv���p�e�B���Q�Ƃ��Ă��������B", false)]
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
    /// EditPanel�̎��s��ԍX�V���b�Z�[�W
    /// </summary>
    [Obsolete("�W��MVVM�����Ɉڍs�B���ڃv���p�e�B��ݒ肵�Ă��������B", false)]
    public class UpdateEditPanelRunningStateMessage
    {
        public bool IsRunning { get; }

        public UpdateEditPanelRunningStateMessage(bool isRunning)
        {
            IsRunning = isRunning;
        }
    }
}