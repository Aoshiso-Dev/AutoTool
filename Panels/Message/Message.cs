﻿using Panels.Model.List.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Message
{
    public class LogMessage
    {
        public string Text { get; set; }
        public LogMessage(string text) { Text = text; }
    }

    public class RunMessage
    {
    }

    public class StopMessage
    {
    }

    public class SaveMessage
    {
    }

    public class LoadMessage
    {
    }

    public class ClearMessage
    {
    }

    public class AddMessage
    {
        public string ItemType { get; set; }
        public AddMessage(string itemType) { ItemType = itemType; }
    }

    public class UpMessage
    {
    }

    public class DownMessage
    {
    }

    public class DeleteMessage
    {
    }

    public class EditMessage
    {
        public ICommandListItem Item { get; set; }
        public EditMessage(ICommandListItem item) { Item = item; }
    }

    public class  ChangeSelectedMessage
    {
        public ICommandListItem Item { get; set; }
        public ChangeSelectedMessage(ICommandListItem item) { Item = item; }
    }

    public class ApplyMessage
    {
    }

    public class ChangeTabMessage
    {
        public int TabIndex { get; set; }
        public ChangeTabMessage(int tabIndex) { TabIndex = tabIndex; }
    }
}
