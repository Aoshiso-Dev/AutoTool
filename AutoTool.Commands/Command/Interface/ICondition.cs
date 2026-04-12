using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Commands.Interface
{
    public interface ICondition
    {
        IConditionSettings Settings { get; }

        Task<bool> Evaluate(CancellationToken cancellationToken);
    }
}

