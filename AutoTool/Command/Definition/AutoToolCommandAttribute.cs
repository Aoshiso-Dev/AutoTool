using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Command.Definition
{
    internal class AutoToolCommandAttribute : Attribute
    {
        public string CommandId { get; }
        public Type Type { get; }

        public AutoToolCommandAttribute(string commandId, Type type)
        {
            CommandId = commandId;
            Type = type;
        }
    }
}