using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Commands.Flow.Break
{
    [Command("Break", "ループ終了", IconKey = "mdi:arrow-right-bold", Category = "フロー制御", Description = "現在のループを終了します", Order = 50)]
    public partial class BreakCommand :
        ObservableObject,
        IAutoToolCommand,
        IHasSettings<BreakSettings>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "Break";
        public string DisplayName => "ループ終了";
        public bool IsEnabled { get; set; } = true;

        [ObservableProperty]
        private BreakSettings settings;

        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<BreakCommand>? _logger;

        public BreakCommand(BreakSettings settings, IServiceProvider? serviceProvider = null)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider?.GetService<ILogger<BreakCommand>>();
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            await Task.Yield(); // 非同期メソッドの形式を保つ

            if (!IsEnabled)
            {
                _logger?.LogDebug("BreakCommand is disabled, continuing to next command");
                return ControlFlow.Next;
            }

            _logger?.LogInformation("Breaking out of current loop");
            return ControlFlow.Break;
        }

        public void ReplaceSettings(BreakSettings next)
        {
            Settings = next ?? throw new ArgumentNullException(nameof(next));
        }
    }
}
