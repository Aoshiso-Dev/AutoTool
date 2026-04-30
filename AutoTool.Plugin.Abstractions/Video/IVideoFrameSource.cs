namespace AutoTool.Plugin.Abstractions.Video;

public interface IVideoFrameSource
{
    string SourceId { get; }

    IAsyncEnumerable<VideoFrame> GetFramesAsync(
        VideoFrameSourceOptions? options,
        CancellationToken cancellationToken);
}
