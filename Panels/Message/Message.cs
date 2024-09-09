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
}
