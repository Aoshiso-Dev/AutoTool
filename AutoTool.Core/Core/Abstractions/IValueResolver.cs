using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Abstractions
{
    public interface IValueResolver
    {
        Task<string?> ResolveStringAsync(object? valueSource, CancellationToken ct);
        Task<bool> EvaluateBoolAsync(string expr, CancellationToken ct);
    }
}
