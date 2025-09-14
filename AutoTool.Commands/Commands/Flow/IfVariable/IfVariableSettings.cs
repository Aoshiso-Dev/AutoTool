using AutoTool.Core.Abstractions;
using System.ComponentModel;
using System.Drawing;

namespace AutoTool.Commands.Flow.IfImageExist;

public sealed class IfVariableSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;

    [Category("基本設定"), DisplayName("変数名")]
    public string VariableName { get; set; } = string.Empty;

    [Category("基本設定"), DisplayName("比較値")]
    public string CompareValue { get; set; } = string.Empty;

    [Category("基本設定"), DisplayName("比較条件")]
    public VariableOperator Operator { get; set; } = VariableOperator.Equals;
}


public enum VariableOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains
}