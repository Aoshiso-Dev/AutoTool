using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Diagnostics
{
    /// <summary>ブレークポイント対応（デバッガ用）。</summary>
    public interface ISupportsBreakpoint
    {
        bool BreakBefore { get; set; } // 実行前に一時停止
        bool BreakAfter { get; set; } // 実行後に一時停止
    }
}
