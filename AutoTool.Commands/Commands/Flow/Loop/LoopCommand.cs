using AutoTool.Commands.Flow.Wait;
using AutoTool.Commands.Input.KeyInput;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.While
{
    [Command("Loop", "ループ", IconKey = "mdi:loop", Category = "フロー制御", Description = "処理を繰り返します", Order = 40)]
    public partial class LoopCommand :
    ObservableObject,
    IAutoToolCommand,
    IHasSettings<LoopSettings>,
    IHasBlocks
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "Loop";
        public string DisplayName => "ループ";
        public bool IsEnabled { get; set; } = true;

        public IServiceProvider? _serviceProvider = null;
        private readonly ILogger<KeyInputCommand>? _logger = null;

        [ObservableProperty]
        private LoopSettings _settings;

        [ObservableProperty]
        private CommandBlock _body;

        public ObservableCollection<CommandBlock> Blocks { get; }

        public LoopCommand(LoopSettings settings, IServiceProvider serviceProvider, IEnumerable<IAutoToolCommand>? body = null)
        {
            Settings = settings;

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetService(typeof(ILogger<KeyInputCommand>)) as ILogger<KeyInputCommand> ?? throw new ArgumentNullException(nameof(_logger));

            Body = new CommandBlock("Body", body);
            Blocks = new ObservableCollection<CommandBlock> { Body };
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            for(int i = 0; i< Settings.LoopCount; i++)
            {
                foreach (var child in Body.Children)
                {
                    var r = await child.ExecuteAsync(ct);
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
