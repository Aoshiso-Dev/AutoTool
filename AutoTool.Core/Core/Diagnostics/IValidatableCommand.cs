using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Diagnostics
{
    /// <summary>構造/設定の検証を提供（編集確定時や実行前に利用）。</summary>
    public interface IValidatableCommand
    {
        IEnumerable<string> Validate(IServiceProvider services);
    }
}
