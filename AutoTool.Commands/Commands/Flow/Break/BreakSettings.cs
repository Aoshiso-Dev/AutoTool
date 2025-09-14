using AutoTool.Core.Abstractions;
using System.ComponentModel;

namespace AutoTool.Commands.Commands.Flow.Break;

public sealed class BreakSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;
}
