using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Abstractions.Video;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// 映像ストリーム registry を介した登録、参照、一覧、解除の契約を確認するテストです。
/// </summary>
public sealed class VideoStreamRegistryTests
{
    [Fact]
    public async Task RegisterAndGetSourceAsync_ReturnsSourceWithoutPluginReference()
    {
        var registry = new VideoStreamRegistry();
        var source = new TestVideoFrameSource("camera.main");

        await registry.RegisterAsync(new VideoStreamRegistration
        {
            SourceId = source.SourceId,
            DisplayName = "Main Camera",
            ProviderPluginId = "Mitaka.Camera",
            Width = 640,
            Height = 480,
            PixelFormat = VideoPixelFormat.Bgr24,
            Source = source,
        });

        var resolved = await registry.GetSourceAsync("camera.main");
        var descriptor = Assert.Single(registry.GetSources());

        Assert.Same(source, resolved);
        Assert.Equal("camera.main", descriptor.SourceId);
        Assert.Equal("Main Camera", descriptor.DisplayName);
        Assert.Equal("Mitaka.Camera", descriptor.ProviderPluginId);
        Assert.Equal(640, descriptor.Width);
        Assert.Equal(480, descriptor.Height);
        Assert.Equal(VideoPixelFormat.Bgr24, descriptor.PixelFormat);
    }

    [Fact]
    public async Task UnregisterAsync_RemovesSource()
    {
        var registry = new VideoStreamRegistry();
        var source = new TestVideoFrameSource("camera.main");
        await registry.RegisterAsync(new VideoStreamRegistration
        {
            SourceId = source.SourceId,
            DisplayName = "Main Camera",
            ProviderPluginId = "Mitaka.Camera",
            Source = source,
        });

        await registry.UnregisterAsync(source.SourceId);

        Assert.Null(await registry.GetSourceAsync(source.SourceId));
        Assert.Empty(registry.GetSources());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateSourceId_RecordsIssueAndThrows()
    {
        var registry = new VideoStreamRegistry();
        await registry.RegisterAsync(new VideoStreamRegistration
        {
            SourceId = "camera.main",
            DisplayName = "Main Camera",
            ProviderPluginId = "Mitaka.Camera",
            Source = new TestVideoFrameSource("camera.main"),
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await registry.RegisterAsync(new VideoStreamRegistration
            {
                SourceId = "camera.main",
                DisplayName = "Duplicated Camera",
                ProviderPluginId = "ImageProcessing.Plugin",
                Source = new TestVideoFrameSource("camera.main"),
            }));

        Assert.Contains("重複", exception.Message, StringComparison.Ordinal);
        var issue = Assert.Single(registry.GetIssues());
        Assert.Equal("camera.main", issue.SourceId);
        Assert.Equal("ImageProcessing.Plugin", issue.ProviderPluginId);
    }

    [Fact]
    public async Task GetFramesAsync_UsesReadOnlyMemoryWithoutCopyingArray()
    {
        var imageData = new byte[] { 1, 2, 3, 4 };
        var source = new TestVideoFrameSource("camera.main", imageData);
        await using var enumerator = source.GetFramesAsync(null, CancellationToken.None).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.True(enumerator.Current.ImageData.Span.SequenceEqual(imageData));

        imageData[0] = 9;
        Assert.Equal(9, enumerator.Current.ImageData.Span[0]);
    }

    [Fact]
    public async Task LoadedPluginCatalogDisposeAsync_UnregistersProviderVideoSources()
    {
        var registry = new VideoStreamRegistry();
        var source = new TestVideoFrameSource("camera.main");
        await registry.RegisterAsync(new VideoStreamRegistration
        {
            SourceId = source.SourceId,
            DisplayName = "Main Camera",
            ProviderPluginId = "Mitaka.Camera",
            Source = source,
        });
        var catalog = new LoadedPluginCatalog(new StaticPluginLoader(CreateLoadedPlugin("Mitaka.Camera")), registry);
        Assert.Single(catalog.GetLoadedPlugins());

        await catalog.DisposeAsync();

        Assert.Empty(registry.GetSources());
    }

    private sealed class TestVideoFrameSource(
        string sourceId,
        byte[]? imageData = null) : IVideoFrameSource
    {
        private readonly byte[] _imageData = imageData ?? [0, 0, 0, 0];

        public string SourceId { get; } = sourceId;

        public async IAsyncEnumerable<VideoFrame> GetFramesAsync(
            VideoFrameSourceOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new VideoFrame(
                _imageData,
                width: 2,
                height: 2,
                pixelFormat: VideoPixelFormat.Gray8,
                timestamp: DateTimeOffset.UnixEpoch,
                frameNumber: 1,
                sourceId: SourceId);
        }
    }

    private static LoadedPlugin CreateLoadedPlugin(string pluginId)
    {
        return new LoadedPlugin
        {
            Manifest = new PluginManifest
            {
                PluginId = pluginId,
                DisplayName = pluginId,
                Version = "1.0.0",
                EntryAssembly = "Test.dll",
                EntryType = "Test.Plugin",
            },
            AssemblyPath = Assembly.GetExecutingAssembly().Location,
            Assembly = Assembly.GetExecutingAssembly(),
            LoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!,
            Instance = new DisposablePlugin(pluginId),
        };
    }

    private sealed class StaticPluginLoader(LoadedPlugin plugin) : IPluginLoader
    {
        public PluginLoadResult Load(PluginManifestLoadResult manifestLoadResult)
        {
            return new PluginLoadResult
            {
                ManifestLoadResult = manifestLoadResult,
                IsLoaded = true,
                Plugin = plugin,
                Errors = [],
            };
        }

        public IReadOnlyList<PluginLoadResult> LoadAll()
        {
            return
            [
                new PluginLoadResult
                {
                    ManifestLoadResult = new PluginManifestLoadResult
                    {
                        PluginDirectoryPath = string.Empty,
                        ManifestPath = string.Empty,
                        IsValid = true,
                        Manifest = plugin.Manifest,
                    },
                    IsLoaded = true,
                    Plugin = plugin,
                    Errors = [],
                }
            ];
        }
    }

    private sealed class DisposablePlugin(string pluginId) : IAutoToolPlugin
    {
        public PluginDescriptor Descriptor { get; } = new()
        {
            PluginId = pluginId,
            DisplayName = pluginId,
            Version = "1.0.0",
            EntryAssembly = "Test.dll",
            EntryType = "Test.Plugin",
            Permissions = [],
        };

        public ValueTask<PluginInitializationResult> InitializeAsync(
            IPluginInitializationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(PluginInitializationResult.Success());
        }

        public ValueTask DisposeAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
