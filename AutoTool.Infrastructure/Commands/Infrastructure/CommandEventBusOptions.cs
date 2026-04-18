namespace AutoTool.Commands.Infrastructure;

public sealed class CommandEventBusOptions
{
    public const string SectionName = "CommandEventBus";
    public int SubscriberBufferSize { get; set; } = 2048;
    public long DropWarningInitialThreshold { get; set; } = 1;
    public long DropWarningInterval { get; set; } = 100;
}
