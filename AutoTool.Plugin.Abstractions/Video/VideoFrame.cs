namespace AutoTool.Plugin.Abstractions.Video;

public sealed class VideoFrame : IDisposable
{
    private readonly IDisposable? _owner;
    private bool _disposed;

    public VideoFrame(
        ReadOnlyMemory<byte> imageData,
        int width,
        int height,
        VideoPixelFormat pixelFormat,
        DateTimeOffset timestamp,
        long frameNumber,
        string sourceId,
        IDisposable? owner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "幅は 1 以上である必要があります。");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "高さは 1 以上である必要があります。");
        }

        ImageData = imageData;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Timestamp = timestamp;
        FrameNumber = frameNumber;
        SourceId = sourceId;
        _owner = owner;
    }

    public ReadOnlyMemory<byte> ImageData { get; }

    public int Width { get; }

    public int Height { get; }

    public VideoPixelFormat PixelFormat { get; }

    public DateTimeOffset Timestamp { get; }

    public long FrameNumber { get; }

    public string SourceId { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _owner?.Dispose();
        _disposed = true;
    }
}
