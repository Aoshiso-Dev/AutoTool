using AutoTool.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Simulation
{
    /// <summary>ドライラン（副作用なしシミュレーション）対応。</summary>
    public interface ISupportsDryRun
    {
        Task<ControlFlow> DryRunAsync(IExecutionContext context, CancellationToken ct);
    }

}
