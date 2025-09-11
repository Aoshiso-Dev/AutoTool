using AutoTool.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 実行コンテキストの実装
/// </summary>
public class ExecutionContext : IExecutionContext
{
    private readonly ILogger<ExecutionContext> _logger;
    private readonly Dictionary<string, object> _variables = new();

    public ExecutionContext(
        ILogger<ExecutionContext> logger,
        IValueResolver valueResolver,
        IVariableScope variables)
    {
        _logger = logger;
        ValueResolver = valueResolver;
        Variables = variables;
        Logger = logger;
        ShutdownToken = CancellationToken.None; // 実際の実装では適切なトークンを設定
    }

    public IValueResolver ValueResolver { get; }
    public IVariableScope Variables { get; }
    public ILogger Logger { get; }
    public CancellationToken ShutdownToken { get; }

    public async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        _logger.LogDebug("Delaying for {Delay}ms", delay.TotalMilliseconds);
        await Task.Delay(delay, ct);
    }
}