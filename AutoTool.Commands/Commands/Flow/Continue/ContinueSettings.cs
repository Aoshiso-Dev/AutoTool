using AutoTool.Core.Abstractions;
using System.ComponentModel;

namespace AutoTool.Commands.Commands.Flow.Continue;

public sealed class ContinueSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;
}
