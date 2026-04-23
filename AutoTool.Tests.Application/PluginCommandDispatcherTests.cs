using System.IO;
using System.Reflection;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグインコマンド実行ディスパッチャがロード済みプラグインへ委譲できることを確認します。
/// </summary>
public sealed class PluginCommandDispatcherTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginCommandDispatcherTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExecuteAsync_WithLoadedPlugin_DelegatesToPluginExecutor()
    {
        var pluginDirectoryPath = Path.Combine(_rootDirectoryPath, "Sample.Plugin");
        Directory.CreateDirectory(pluginDirectoryPath);
        CopySamplePluginAssembly(pluginDirectoryPath);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Sample.Plugin",
              "displayName": "Sample Plugin",
              "version": "1.0.0",
              "entryAssembly": "AutoTool.Tests.Plugin.Sample.dll",
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin"
            }
            """);

        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);
        ILoadedPluginCatalog loadedPluginCatalog = new LoadedPluginCatalog(loader);
        IPluginCommandDispatcher dispatcher = new PluginCommandDispatcher(loadedPluginCatalog);

        var context = new FakeCommandExecutionContext();
        var item = new PluginCommandListItem
        {
            ItemType = "Sample.Plugin.ProviderCommand",
            PluginId = "Sample.Plugin",
            ParameterJson = """{"targetVariable":"result","value":"ok"}""",
        };

        var success = await dispatcher.ExecuteAsync(item, context, CancellationToken.None);

        Assert.True(success);
        Assert.Equal("ok", context.GetVariable("result"));
        Assert.Contains("provider command executed", context.Logs);
    }

    public void Dispose()
    {
    }

    private static void CopySamplePluginAssembly(string pluginDirectoryPath)
    {
        var assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(AutoTool.Tests.Plugin.Sample.SamplePlugin))!.Location)!;
        File.Copy(
            Path.Combine(assemblyDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            Path.Combine(pluginDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            overwrite: true);
    }

    private sealed class FakeCommandExecutionContext : ICommandExecutionContext
    {
        private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);

        public List<string> Logs { get; } = [];

        public DateTimeOffset GetLocalNow() => DateTimeOffset.Parse("2026-04-23T12:00:00+09:00");

        public void ReportProgress(int progress)
        {
        }

        public void Log(string message)
        {
            Logs.Add(message);
        }

        public string? GetVariable(string name) => _variables.TryGetValue(name, out var value) ? value : null;

        public void SetVariable(string name, string value)
        {
            _variables[name] = value;
        }

        public string ToAbsolutePath(string relativePath) => Path.GetFullPath(relativePath);

        public Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false) => Task.CompletedTask;

        public Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null) => Task.CompletedTask;

        public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) => Task.FromResult<MatchPoint?>(null);

        public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true)
        {
        }

        public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold) => [];

        public int ResolveAiClassId(string modelPath, int fallbackClassId, string? labelName, string? labelsPath) => fallbackClassId;

        public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new OcrExtractionResult(string.Empty, 0));
    }
}

