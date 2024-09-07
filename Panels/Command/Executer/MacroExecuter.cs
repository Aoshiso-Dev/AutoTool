using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.Command.Interface;

namespace Panels.Command.Executer
{
    public static class MacroExecuter
    {
        public static void Execute(IRootCommand macro, CancellationToken cancellationToken)
        {
            Exception? e = null;

            foreach (var command in macro.Children)
            {
                if (!command.Execute(cancellationToken))
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;

                }
            }
        }
    }
}
