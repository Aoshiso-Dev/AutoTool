using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.If
{
    public sealed class IfCommand :
    IAutoToolCommand,
    IHasSettings<IfSettings>,
    IHasBlocks,
    IValidatableCommand,
    IDeepCloneable<IfCommand>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "if";
        public string DisplayName => "If";
        public bool IsEnabled { get; set; } = true;

        public IfSettings Settings { get; }

        private readonly CommandBlock _then;
        private readonly CommandBlock _else;
        public IReadOnlyList<CommandBlock> Blocks { get; }

        public IfCommand(IfSettings settings,
                         IEnumerable<IAutoToolCommand>? then = null,
                         IEnumerable<IAutoToolCommand>? @else = null)
        {
            Settings = settings;
            _then = new CommandBlock("Then", then);
            _else = new CommandBlock("Else", @else);
            Blocks = new[] { _then, _else };
        }

        public async Task<ControlFlow> ExecuteAsync(IExecutionContext ctx, CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            var cond = await ctx.ValueResolver.EvaluateBoolAsync(Settings.ConditionExpr, ct);
            var target = cond ? _then.Children : _else.Children;

            foreach (var child in target)
            {
                var r = await child.ExecuteAsync(ctx, ct);
                if (r is not ControlFlow.Next) return r;
            }
            return ControlFlow.Next;
        }

        public IEnumerable<string> Validate(IServiceProvider _)
        {
            if (string.IsNullOrWhiteSpace(Settings.ConditionExpr))
                yield return "条件式は必須です。";
        }

        public IfCommand DeepClone()
            => new IfCommand(Settings with { }, _then.Children.Select(Clone), _else.Children.Select(Clone));

        private static IAutoToolCommand Clone(IAutoToolCommand c)
            => (c as IDeepCloneable<IAutoToolCommand>)?.DeepClone() ?? c; // フォールバック
    }
}
