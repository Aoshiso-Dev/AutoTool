namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// 機能動作に利用するオプション設定値を保持します。
/// </summary>
public sealed class CommandEventBusOptions
{
    public const string SectionName = "CommandEventBus";
    public int SubscriberBufferSize { get; set; } = 2048;
    public long DropWarningInitialThreshold { get; set; } = 1;
    public long DropWarningInterval { get; set; } = 100;
}
