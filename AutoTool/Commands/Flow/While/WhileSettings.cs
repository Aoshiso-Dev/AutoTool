using AutoTool.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.While
{
    public sealed class WhileSettings : IAutoToolCommandSettings
    {
        public int Version { get; init; } = 1;
        public string ConditionExpr { get; init; } = "true";
        public int MaxIterations { get; init; } = 10_000; // 暴走防止
    }
}
