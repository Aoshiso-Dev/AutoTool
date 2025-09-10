using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Utilities
{
    /// <summary>ディープクローン（コピー/テンプレート展開用）。</summary>
    public interface IDeepCloneable<out T>
    {
        T DeepClone();
    }
}
