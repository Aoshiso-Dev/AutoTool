using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(LoopBreakCommand), typeof(LoopBreakCommand))]
    public class LoopBreakCommand : BaseCommand
    {
        public LoopBreakCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ループ中断";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage("ループ中断を要求します");
            throw new LoopBreakException("ループ中断コマンドが実行されました");
        }
    }
}
