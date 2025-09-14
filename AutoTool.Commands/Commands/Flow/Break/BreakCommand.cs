using AutoTool.Commands.Input.KeyInput;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Commands.Flow.Break;

[Command("Break", "ループ終了", IconKey = "mdi:TrendingNeutral", Category = "フロー制御", Description = "現在のループを終了します", Order = 40)]
public partial class BreakCommand :
    ObservableObject,
    IHasSettings<BreakSettings>,
    IAutoToolCommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "Break";
    public string DisplayName => "ループ終了";
    public bool IsEnabled { get; set; } = true;

    public BreakSettings Settings { get; init; }

    public IServiceProvider? _serviceProvider = null;
    private readonly ILogger<KeyInputCommand>? _logger = null;

    public BreakCommand(BreakSettings settings, IServiceProvider serviceProvider)
    {
        Settings = settings;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetService(typeof(ILogger<KeyInputCommand>)) as ILogger<KeyInputCommand> ?? throw new ArgumentNullException(nameof(_logger));
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;

        return ControlFlow.Break;
    }
}
