using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Panels.Command.Interface;
using Panels.Command.Class;
using Panels.Command.Define;

namespace Panels.Command.Factory
{
    internal class MacroFactory
    {
        public static IRootCommand CreateMacro()
        {
            return new RootCommand(new ClickCommandSettings(),new List<BaseCommand>());
        }
    }
}
