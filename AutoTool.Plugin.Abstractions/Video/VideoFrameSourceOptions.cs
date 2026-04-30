namespace AutoTool.Plugin.Abstractions.Video;

public sealed record VideoFrameSourceOptions
{
    public int? RequestedWidth { get; init; }

    public int? RequestedHeight { get; init; }

    public VideoPixelFormat? RequestedPixelFormat { get; init; }

    public double? MaxFrameRate { get; init; }

    public long? StartFrameNumber { get; init; }
}
