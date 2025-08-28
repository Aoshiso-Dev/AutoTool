using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;

namespace MacroPanels.Command.Message
{
    public class DoingCommandEventArgs : EventArgs
    {
        public string Detail { get; set; }
        public DoingCommandEventArgs(string detail) { Detail = detail; }
    }


    public class StartCommandMessage
    {
        public ICommand Command { get; set; }
        public StartCommandMessage(ICommand command) { Command = command; }
    }

    public class FinishCommandMessage
    {
        public ICommand Command { get; set; }
        public FinishCommandMessage(ICommand command) { Command = command; }
    }

    public class DoingCommandMessage
    {
        public ICommand Command { get; set; }
        public string Detail { get; set; }
        public DoingCommandMessage(ICommand command, string detail) { Command = command; Detail = detail; }
    }

    public class  UpdateProgressMessage
    {
        public ICommand Command { get; set; }
        public int Progress { get; set; }
        public UpdateProgressMessage(ICommand command, int progress) { Command = command;  Progress = progress; }
    }
}
