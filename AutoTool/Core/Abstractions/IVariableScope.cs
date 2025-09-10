using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Abstractions
{
    public interface IVariableScope
    {
        bool TryGet(string name, out object? value);
        void Set(string name, object? value);
    }

}
