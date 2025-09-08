using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(IfVariableCommand), typeof(IfVariableCommand))]
    public class IfVariableCommand : IfCommand
    {
        [Category("基本設定"), DisplayName("変数名")]
        public string VariableName { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("演算子")]
        public string Operator { get; set; } = "==";

        [Category("基本設定"), DisplayName("比較値")]
        public string VariableValue { get; set; } = string.Empty;

        private readonly IVariableStoreService? _variableStore;

        public IfVariableCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "変数条件確認";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(VariableName)) 
            {
                LogMessage("変数名が設定されていません");
                return false;
            }

            var lhs = _variableStore?.Get(VariableName) ?? string.Empty;
            var rhs = VariableValue ?? string.Empty;

            LogMessage($"変数条件を評価中: {VariableName}({lhs}) {Operator} {rhs}");

            bool result = Evaluate(lhs, rhs, Operator);
            
            LogMessage($"変数条件の結果: {VariableName}({lhs}) {Operator} {rhs} => {(result ? "真" : "偽")}");

            return result;
        }

        private static bool Evaluate(string lhs, string rhs, string op)
        {
            op = (op ?? "").Trim();
            if (double.TryParse(lhs, out var lnum) && double.TryParse(rhs, out var rnum))
            {
                return op switch
                {
                    "==" => lnum == rnum,
                    "!=" => lnum != rnum,
                    ">" => lnum > rnum,
                    "<" => lnum < rnum,
                    ">=" => lnum >= rnum,
                    "<=" => lnum <= rnum,
                    _ => throw new Exception($"不明な数値比較演算子です: {op}"),
                };
            }
            else
            {
                return op switch
                {
                    "==" => string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "!=" => !string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "Contains" => lhs.Contains(rhs, StringComparison.Ordinal),
                    "StartsWith" => lhs.StartsWith(rhs, StringComparison.Ordinal),
                    "EndsWith" => lhs.EndsWith(rhs, StringComparison.Ordinal),
                    "IsEmpty" => string.IsNullOrEmpty(lhs),
                    "IsNotEmpty" => !string.IsNullOrEmpty(lhs),
                    _ => throw new Exception($"不明な文字列比較演算子です: {op}"),
                };
            }
        }
    }
}
