using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(IfEndCommand), typeof(IfEndCommand))]
    public class IfEndCommand : BaseCommand
    {
        public IfEndCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "IfèIóπ";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            ResetChildrenProgress();
            return Task.FromResult(true);
        }
    }
}
