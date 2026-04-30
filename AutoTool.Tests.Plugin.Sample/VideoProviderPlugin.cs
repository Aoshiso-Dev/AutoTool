using System.Runtime.CompilerServices;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Abstractions.Video;

namespace AutoTool.Tests.Plugin.Sample;

public sealed class VideoProviderPlugin : IAutoToolPlugin
{
    public PluginDescriptor Descriptor { get; } = new()
    {
        PluginId = "Sample.VideoProvider",
        DisplayName = "Sample Video Provider",
        Version = "1.0.0",
        EntryAssembly = "AutoTool.Tests.Plugin.Sample.dll",
        EntryType = "AutoTool.Tests.Plugin.Sample.VideoProviderPlugin",
        Permissions = [],
    };

    public async ValueTask<PluginInitializationResult> InitializeAsync(
        IPluginInitializationContext context,
        CancellationToken cancellationToken)
    {
        await context.VideoStreams.RegisterAsync(new VideoStreamRegistration
        {
            SourceId = "sample.camera",
            DisplayName = "Sample Camera",
            ProviderPluginId = "Sample.VideoProvider",
            Width = 320,
            Height = 240,
            PixelFormat = VideoPixelFormat.Gray8,
            Source = new SampleVideoFrameSource("sample.camera"),
        }, cancellationToken).ConfigureAwait(false);

        return PluginInitializationResult.Success();
    }

    public ValueTask DisposeAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private sealed class SampleVideoFrameSource(string sourceId) : IVideoFrameSource
    {
        public string SourceId { get; } = sourceId;

        public async IAsyncEnumerable<VideoFrame> GetFramesAsync(
            VideoFrameSourceOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new VideoFrame(
                new byte[] { 0 },
                width: 1,
                height: 1,
                pixelFormat: VideoPixelFormat.Gray8,
                timestamp: DateTimeOffset.UnixEpoch,
                frameNumber: 1,
                sourceId: SourceId);
        }
    }
}
