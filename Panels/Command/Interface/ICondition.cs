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
        bool Evaluate(out Exception? exception);
    }

    public interface IImageCondition : ICondition
    {
        IImageConditionSettings Settings { get; }
    }
}
