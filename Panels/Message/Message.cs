using MacroPanels.Model.List.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPanels.Message
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

    public class  ChangeSelectedMessage
    {
        public ICommandListItem? Item { get; set; }
        public ChangeSelectedMessage(ICommandListItem? item) { Item = item; }
    }

    public class EditCommandMessage
    {
        public ICommandListItem? Item { get; set; }
        public EditCommandMessage(ICommandListItem? item) { Item = item; }
    }

        public class RefreshListViewMessage
    {
    }
}
