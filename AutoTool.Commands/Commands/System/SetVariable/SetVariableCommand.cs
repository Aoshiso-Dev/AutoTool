using AutoTool.Commands.Input.KeyInput;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using AutoTool.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.Commands.Commands.System.SetVariable;

/// <summary>
/// 変数を設定するコマンド
/// </summary>
[Command("SetVariable", "変数設定", IconKey = "mdi:variable", Category = "変数操作", Description = "指定した変数に値を設定します", Order = 25)]
internal class SetVariableCommand :
    IAutoToolCommand,
    IHasSettings<SetVariableSettings>,
    IValidatableCommand
{
    private IServiceProvider? _serviceProvider = null;
    private readonly ILogger _logger;
    private readonly IVariableScope _variableService;

    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "SetVariable";
    public string DisplayName => "変数設定";
    public bool IsEnabled { get; set; } = true;
    public SetVariableSettings Settings { get; private set; }

    public SetVariableCommand(SetVariableSettings settings, IServiceProvider serviceProvider)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService(typeof(ILogger<SetVariableCommand>)) as ILogger<SetVariableCommand>
            ?? throw new ArgumentNullException(nameof(_logger));
        _variableService = serviceProvider.GetService(typeof(IVariableScope)) as IVariableScope
            ?? throw new ArgumentNullException(nameof(_variableService));
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;
        try
        {
            // 変数を設定
            _variableService.Set(Settings.VariableName, Settings.VariableValue);
            _logger.LogInformation("Set variable {VariableName} to {VariableValue}", Settings.VariableName, Settings.VariableValue);
            await Task.CompletedTask;
            return ControlFlow.Next;
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は停止を返す
            return ControlFlow.Stop;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set variable failed: {VariableName}", Settings.VariableName);
            return ControlFlow.Error;
        }
    }

    public IEnumerable<string> Validate(IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(Settings.VariableName))
            yield return "変数名が指定されていません。";
        // 必要に応じて他のバリデーションを追加
    }
}
