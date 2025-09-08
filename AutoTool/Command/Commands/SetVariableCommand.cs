using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(SetVariableCommand), typeof(SetVariableCommand))]
    public class SetVariableCommand : BaseCommand
    {
        [Category("Šî–{İ’è"), DisplayName("•Ï”–¼")]
        public string VariableName { get; set; } = string.Empty;

        [Category("Šî–{İ’è"), DisplayName("•Ï”’l")]
        public string VariableValue { get; set; } = string.Empty;

        private readonly IVariableStoreService? _variableStore;

        public SetVariableCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "•Ï”‘ã“ü";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(VariableName)) return false;

            _variableStore?.Set(VariableName, VariableValue);
            LogMessage($"•Ï”‚ğ‘ã“ü‚µ‚Ü‚µ‚½: {VariableName} = \"{VariableValue}\"");
            return true;
        }
    }
}
