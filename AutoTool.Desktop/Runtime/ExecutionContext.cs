using AutoTool.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.Runtime;
/*
public class ExecutionContext : IExecutionContext
{
    public IValueResolver ValueResolver { get; }
    public IVariableScope Variables { get; }
    public ILogger Logger { get; }
    public CancellationToken ShutdownToken { get; }

    public ExecutionContext(
        IValueResolver valueResolver,
        IVariableScope variables,
        ILogger<ExecutionContext> logger,
        CancellationToken shutdownToken = default)
    {
        ValueResolver = valueResolver ?? throw new ArgumentNullException(nameof(valueResolver));
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ShutdownToken = shutdownToken;
    }

    public async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        await Task.Delay(delay, ct);
    }
}
    */