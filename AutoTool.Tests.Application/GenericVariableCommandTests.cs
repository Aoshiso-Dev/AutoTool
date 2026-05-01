using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Automation.Runtime.Tests;

public class GenericVariableCommandTests
{
    [Fact]
    public async Task ExtractJsonValueItem_WhenDetectionValueNameMatches_StoresValue()
    {
        var context = new TestCommandExecutionContext();
        context.SetVariable("imageJson", """
            [
              {
                "DetectionValues": [
                  { "Name": "EdgeX", "Value": 10.5 },
                  { "Name": "EdgeY", "Value": 20.5 }
                ]
              },
              {
                "DetectionValues": [
                  { "Name": "EdgeX", "Value": 30.25 },
                  { "Name": "EdgeY", "Value": 40.25 }
                ]
              }
            ]
            """);

        var item = new ExtractJsonValueItem
        {
            JsonVariableName = "imageJson",
            ExtractionPath = "[last].DetectionValues[Name=EdgeX].Value",
            OutputVariableName = "edgeX"
        };

        var result = await item.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("30.25", context.GetVariable("edgeX"));
    }

    [Fact]
    public async Task CalculateVariableItem_WhenExpressionUsesVariables_StoresResult()
    {
        var context = new TestCommandExecutionContext();
        context.SetVariable("edgeX", "30.25");
        context.SetVariable("edgeX0", "10.25");
        context.SetVariable("pixelSizeUm", "2.5");

        var item = new CalculateVariableItem
        {
            Expression = "(edgeX - edgeX0) * pixelSizeUm",
            OutputVariableName = "deltaUm"
        };

        var result = await item.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("50", context.GetVariable("deltaUm"));
    }

    [Fact]
    public async Task AppendCsvItem_WhenFileIsEmpty_WritesHeaderAndVariableValues()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"autotool-csv-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new TestCommandExecutionContext(tempDir);
            context.SetVariable("step", "1");
            context.SetVariable("machineX", "10");
            context.SetVariable("edgeX", "30.25");
            context.SetVariable("edgeDelta", "20");
            context.SetVariable("deltaUm", "50");

            var item = new AppendCsvItem
            {
                OutputFilePath = "glass-scale.csv",
                HeaderLine = "step,machineX,edgeX,edgeDelta,deltaUm",
                Values = "step,machineX,edgeX,edgeDelta,deltaUm",
                WriteHeaderOnce = true
            };

            var result = await item.ExecuteAsync(context, CancellationToken.None);

            Assert.True(result);
            var lines = await File.ReadAllLinesAsync(Path.Combine(tempDir, "glass-scale.csv"));
            Assert.Equal(
                [
                    "step,machineX,edgeX,edgeDelta,deltaUm",
                    "1,10,30.25,20,50"
                ],
                lines);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private sealed class TestCommandExecutionContext(string? baseDirectory = null) : ICommandExecutionContext
    {
        private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);
        private readonly string _baseDirectory = baseDirectory ?? Path.GetTempPath();

        public DateTimeOffset GetLocalNow() => DateTimeOffset.Now;

        public void ReportProgress(int progress) { }

        public void Log(string message) { }

        public string? GetVariable(string name) => _variables.GetValueOrDefault(name);

        public void SetVariable(string name, string value) => _variables[name] = value;

        public string ToAbsolutePath(string relativePath) =>
            Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(_baseDirectory, relativePath);

        public Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false, bool restoreCursorPositionAfterClick = false, bool restoreWindowZOrderAfterClick = false) =>
            Task.CompletedTask;

        public Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null) =>
            Task.CompletedTask;

        public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) =>
            Task.FromResult<MatchPoint?>(null);

        public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true) { }

        public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold) => [];

        public int ResolveAiClassId(string modelPath, int fallbackClassId, string? labelName, string? labelsPath) => fallbackClassId;

        public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new OcrExtractionResult(string.Empty, 0));
    }
}
