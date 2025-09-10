using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;

namespace AutoTool.Core.Runtime;

public interface ICommandRunner
{
    event EventHandler<IAutoToolCommand>? CommandStarting;
    event EventHandler<(IAutoToolCommand cmd, ControlFlow result)>? CommandFinished;

    Task<ControlFlow> RunAsync(IEnumerable<IAutoToolCommand> root, IExecutionContext ctx, CancellationToken ct);
}