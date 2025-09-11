using AutoTool.Core.Abstractions;

namespace AutoTool.Desktop.Runtime;

public class SimpleValueResolver : IValueResolver
{
    public Task<string?> ResolveStringAsync(object? valueSource, CancellationToken ct)
    {
        return Task.FromResult(valueSource?.ToString());
    }

    public Task<bool> EvaluateBoolAsync(string expr, CancellationToken ct)
    {
        if (bool.TryParse(expr, out var result))
            return Task.FromResult(result);
        
        // 簡単な式評価（実装を拡張可能）
        return Task.FromResult(!string.IsNullOrWhiteSpace(expr));
    }
}