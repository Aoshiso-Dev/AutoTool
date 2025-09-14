using AutoTool.Core.Abstractions;
using System.ComponentModel;

namespace AutoTool.Commands.Flow.Wait;

/// <summary>
/// 待機コマンドの設定
/// </summary>
public sealed class WaitSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;

    [Category("基本設定"), DisplayName("待機時間"), ]
    public int DurationMs { get; set; } = 1000;
}
