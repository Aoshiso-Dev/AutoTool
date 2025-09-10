using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.While
{
    public sealed class WhileCommand :
    IAutoToolCommand,
    IHasSettings<WhileSettings>,
    IHasBlocks
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "while";
        public string DisplayName => "While";
        public bool IsEnabled { get; set; } = true;

        public WhileSettings Settings { get; }
        private readonly CommandBlock _body;
        public IReadOnlyList<CommandBlock> Blocks { get; }

        public WhileCommand(WhileSettings settings, IEnumerable<IAutoToolCommand>? body = null)
        {
            Settings = settings;
            _body = new CommandBlock("Body", body);
            Blocks = new[] { _body };
        }

        public async Task<ControlFlow> ExecuteAsync(IExecutionContext ctx, CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            var i = 0;
            while (await ctx.ValueResolver.EvaluateBoolAsync(Settings.ConditionExpr, ct))
            {
                if (i++ > Settings.MaxIterations)
                    return ControlFlow.Error;

                foreach (var child in _body.Children)
                {
                    var r = await child.ExecuteAsync(ctx, ct);
                    if (r == ControlFlow.Break) return ControlFlow.Next;
                    if (r == ControlFlow.Continue) break;
                    if (r is ControlFlow.Stop or ControlFlow.Error) return r;
                }
                ct.ThrowIfCancellationRequested();
            }
            return ControlFlow.Next;
        }
    }
}
