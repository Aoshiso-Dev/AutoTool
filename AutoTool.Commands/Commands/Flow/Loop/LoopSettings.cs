using AutoTool.Core.Abstractions;
using System.ComponentModel;

public sealed class LoopSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;

    [Category("基本設定"), DisplayName("ループ回数"),]
    public int LoopCount { get; set; } = 3;
}