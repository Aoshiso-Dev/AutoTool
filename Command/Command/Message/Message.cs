using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command.Interface;

namespace Command.Message
{
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

    public class  UpdateProgressMessage
    {
        public ICommand Command { get; set; }
        public int Progress { get; set; }
        public UpdateProgressMessage(ICommand command, int progress) { Command = command;  Progress = progress; }
    }
}
