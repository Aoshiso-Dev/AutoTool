using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using AutoTool.Services.Abstractions;
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
using System.Windows;

namespace AutoTool.Commands.Flow.IfImageExist;

[Command("IfVariable", "条件分岐（変数）", IconKey = "mdi:code-braces", Category = "フロー制御", Description = "変数に応じて処理を分岐します", Order = 30)]
public sealed class IfVariableCommand :
ObservableObject,
IAutoToolCommand,
IHasSettings<IfVariableSettings>,
IHasBlocks,
IValidatableCommand,
INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "IfVariable";
    public string DisplayName => "条件分岐（変数）";
    public bool IsEnabled { get; set; } = true;

    public IfVariableSettings Settings { get; private set; }

    private readonly CommandBlock _then;
    private readonly CommandBlock _else;
    public ObservableCollection<CommandBlock> Blocks { get; }

    private readonly IServiceProvider? _serviceProvider = null;
    private readonly ILogger<IfImageExistCommand>? _logger = null;
    private readonly IVariableScope? _variable = null;

    public IfVariableCommand(IfVariableSettings settings,
                     IServiceProvider? serviceProvider,
                     IEnumerable<IAutoToolCommand>? then = null,
                     IEnumerable<IAutoToolCommand>? @else = null)
    {
        Settings = settings;

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetService(typeof(ILogger<IfImageExistCommand>)) as ILogger<IfImageExistCommand> ?? throw new ArgumentNullException(nameof(_logger));
        _variable = _serviceProvider.GetService(typeof(IVariableScope)) as IVariableScope ?? throw new ArgumentNullException(nameof(_variable));

        _then = new CommandBlock("Then", then);
        _else = new CommandBlock("Else", @else);
        Blocks = new ObservableCollection<CommandBlock> { _then, _else };
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;
        if (_variable == null) throw new InvalidOperationException("VariableService is not set.");

        var ui = _serviceProvider?.GetService(typeof(IUIService)) as IUIService;
        ui?.ShowToast("IfVariableCommand");

        var conditionResult = await EvaluateConditionAsync(ct);
        var target = conditionResult ? _then.Children : _else.Children;

        foreach (var child in target)
        {
            var r = await child.ExecuteAsync(ct);
            if (r is not ControlFlow.Next) return r;
        }
        return ControlFlow.Next;
    }

    public async Task<bool> EvaluateConditionAsync(CancellationToken ct)
    {
        if (_variable == null)
        {
            throw new InvalidOperationException("VariableService is not set.");
        }
        if (_variable.TryGet(Settings.VariableName, out var value) == false)
        {
            throw new InvalidOperationException($"Variable '{Settings.VariableName}' is not found.");
        }

        switch(Settings.Operator)
        {
            case VariableOperator.Equals:
                return string.Equals(value?.ToString(), Settings.CompareValue, StringComparison.OrdinalIgnoreCase);
            case VariableOperator.NotEquals:
                return !string.Equals(value?.ToString(), Settings.CompareValue, StringComparison.OrdinalIgnoreCase);
            case VariableOperator.Contains:
                return value?.ToString()?.Contains(Settings.CompareValue, StringComparison.OrdinalIgnoreCase) == true;
            case VariableOperator.NotContains:
                return value?.ToString()?.Contains(Settings.CompareValue, StringComparison.OrdinalIgnoreCase) == false;
            case VariableOperator.GreaterThan:
                if (double.TryParse(value?.ToString(), out var numValue) && double.TryParse(Settings.CompareValue, out var numCompare))
                {
                    return numValue > numCompare;
                }
                throw new InvalidOperationException($"Cannot compare variable '{Settings.VariableName}' with value '{value}' as number.");
            case VariableOperator.LessThan:
                if (double.TryParse(value?.ToString(), out var numValue2) && double.TryParse(Settings.CompareValue, out var numCompare2))
                {
                    return numValue2 < numCompare2;
                }
                throw new InvalidOperationException($"Cannot compare variable '{Settings.VariableName}' with value '{value}' as number.");
            case VariableOperator.GreaterThanOrEqual:
                if (double.TryParse(value?.ToString(), out var numValue3) && double.TryParse(Settings.CompareValue, out var numCompare3))
                {
                    return numValue3 >= numCompare3;
                }
                throw new InvalidOperationException($"Cannot compare variable '{Settings.VariableName}' with value '{value}' as number.");
            case VariableOperator.LessThanOrEqual:
                if (double.TryParse(value?.ToString(), out var numValue4) && double.TryParse(Settings.CompareValue, out var numCompare4))
                {
                    return numValue4 <= numCompare4;
                }
                throw new InvalidOperationException($"Cannot compare variable '{Settings.VariableName}' with value '{value}' as number.");
            default:
                throw new NotSupportedException($"Operator '{Settings.Operator}' is not supported.");
        }
    }

    public IEnumerable<string> Validate(IServiceProvider _)
    {
        if (string.IsNullOrWhiteSpace(Settings.VariableName))
        {
            yield return "変数名が指定されていません。";
        }
    }
}