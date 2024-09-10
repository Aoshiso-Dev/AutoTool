using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command.Interface;

namespace Command.Message
{
    public class ExecuteCommandMessage
    {
        public ICommand Command { get; set; }
        public ExecuteCommandMessage(ICommand command) { Command = command; }
    }
}
