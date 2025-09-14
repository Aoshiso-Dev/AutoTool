using AutoTool.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace AutoTool.Commands.Commands.System.SetVariable
{
    public sealed class SetVariableSettings : AutoToolCommandSettings
    {
        [Browsable(false)]
        new public int Version { get; init; } = 1;

        [Category("基本設定"), DisplayName("変数名")]
        public string VariableName { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("値")]
        public string VariableValue { get; set; } = string.Empty;
    }
}
