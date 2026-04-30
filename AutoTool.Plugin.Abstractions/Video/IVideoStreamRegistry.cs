namespace AutoTool.Plugin.Abstractions.Video;

public interface IVideoStreamRegistry
{
    ValueTask RegisterAsync(
        VideoStreamRegistration registration,
        CancellationToken cancellationToken = default);

    ValueTask UnregisterAsync(
        string sourceId,
        CancellationToken cancellationToken = default);

    ValueTask<IVideoFrameSource?> GetSourceAsync(
        string sourceId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<VideoStreamDescriptor> GetSources();
}
