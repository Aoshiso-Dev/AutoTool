using AutoTool.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.If
{
    public sealed class IfSettings : IAutoToolCommandSettings
    {
        public int Version { get; init; } = 1;
        public string ConditionExpr { get; init; } = "true";
    }
}
