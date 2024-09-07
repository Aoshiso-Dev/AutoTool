using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Panels.Command.Interface
{
    public interface ICondition
    {
        IConditionSettings Settings { get; }

        Task<bool> Evaluate(CancellationToken cancellationToken);
    }
}
