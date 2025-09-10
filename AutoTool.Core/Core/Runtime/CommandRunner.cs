using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;

namespace AutoTool.Core.Runtime;

public sealed class CommandRunner : ICommandRunner
{
    public event EventHandler<IAutoToolCommand>? CommandStarting;
    public event EventHandler<(IAutoToolCommand cmd, ControlFlow result)>? CommandFinished;

    public async Task<ControlFlow> RunAsync(IEnumerable<IAutoToolCommand> root, IExecutionContext ctx, CancellationToken ct)
    {
        foreach (var cmd in root)
        {
            ct.ThrowIfCancellationRequested();
            if (!cmd.IsEnabled) continue;

            CommandStarting?.Invoke(this, cmd);
            var r = await cmd.ExecuteAsync(ctx, ct);
            CommandFinished?.Invoke(this, (cmd, r));

            if (r is ControlFlow.Break or ControlFlow.Continue or ControlFlow.Stop or ControlFlow.Error)
                return r;
        }
        return ControlFlow.Next;
    }
}