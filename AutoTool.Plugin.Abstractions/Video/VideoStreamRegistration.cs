namespace AutoTool.Plugin.Abstractions.Video;

public sealed record VideoStreamRegistration
{
    public required string SourceId { get; init; }

    public required string DisplayName { get; init; }

    public required string ProviderPluginId { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public VideoPixelFormat? PixelFormat { get; init; }

    public required IVideoFrameSource Source { get; init; }
}
