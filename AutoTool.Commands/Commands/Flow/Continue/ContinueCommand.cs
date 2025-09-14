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

namespace AutoTool.Commands.Commands.Flow.Continue;

[Command("Continue", "ループ中断", IconKey = "mdi:TrendingNeutral", Category = "フロー制御", Description = "現在のループ内の以降のコマンドをスキップして次回ループへ進みます", Order = 40)]
public partial class ContinueCommand :
    ObservableObject,
    IHasSettings<ContinueSettings>,
    IAutoToolCommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "Continue";
    public string DisplayName => "ループ中断";
    public bool IsEnabled { get; set; } = true;

    public ContinueSettings Settings { get; init; }

    public IServiceProvider? _serviceProvider = null;
    private readonly ILogger<ContinueCommand>? _logger = null;

    public ContinueCommand(ContinueSettings settings, IServiceProvider serviceProvider)
    {
        Settings = settings;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetService(typeof(ILogger<ContinueCommand>)) as ILogger<ContinueCommand> ?? throw new ArgumentNullException(nameof(_logger));
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;

        return ControlFlow.Continue;
    }
}
