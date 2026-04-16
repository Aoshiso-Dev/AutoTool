using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using System.IO;

namespace AutoTool.Core.Tests;

public class CommandSettingsValidatorTests
{
    [Fact]
    public void GetIssues_WhenImagePathMissing_ReturnsImagePathRequired()
    {
        var settings = new WaitImageCommandSettings
        {
            ImagePath = string.Empty
        };

        var issues = CommandSettingsValidator.GetIssues(settings);

        Assert.Contains(issues, x => x.Code == CommandValidationErrorCodes.ImagePathRequired);
    }

    [Fact]
    public void GetIssues_WhenIfVariableOperatorInvalid_ReturnsOperatorIssue()
    {
        var settings = new IfVariableCommandSettings
        {
            Name = "A",
            Operator = "Contains"
        };

        var issues = CommandSettingsValidator.GetIssues(settings);

        Assert.Contains(issues, x => x.Code == CommandValidationErrorCodes.VariableOperatorInvalid);
    }

    [Fact]
    public void GetIssues_WhenProgramFileNotFound_ReturnsProgramPathNotFound()
    {
        var settings = new ExecuteCommandSettings
        {
            ProgramPath = "missing.exe"
        };

        var resolver = new TestPathResolver(Path.GetTempPath());
        var issues = CommandSettingsValidator.GetIssues(settings, resolver, includeExistenceChecks: true);

        Assert.Contains(issues, x => x.Code == CommandValidationErrorCodes.ProgramPathNotFound);
    }

    [Fact]
    public void GetIssues_WhenTessdataHasNoTrainedData_ReturnsTessdataIssue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"tessdata-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var settings = new SetVariableOCRCommandSettings
            {
                Name = "ocr",
                Width = 100,
                Height = 50,
                MinConfidence = 50,
                TessdataPath = tempDir
            };

            var resolver = new TestPathResolver(Path.GetTempPath());
            var issues = CommandSettingsValidator.GetIssues(settings, resolver, includeExistenceChecks: true);

            Assert.Contains(issues, x => x.Code == CommandValidationErrorCodes.TessdataDataMissing);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FindImageExecutor_WhenImageMissing_ThrowsValidationExceptionWithCode()
    {
        var options = new FindImageOptions
        {
            ImagePath = Path.Combine(Path.GetTempPath(), $"img-{Guid.NewGuid()}.png"),
            Timeout = 0,
            Interval = 0
        };

        var ex = await Assert.ThrowsAsync<CommandSettingsValidationException>(() =>
            FindImageExecutor.ExecuteAsync(
                options,
                (_, _, _, _, _, _) => Task.FromResult<MatchPoint?>(null),
                _ => { },
                CancellationToken.None));

        Assert.Equal(CommandValidationErrorCodes.ImagePathNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task BaseCommand_WhenValidationFails_ThrowsCommandValidationException()
    {
        var command = new TestValidationCommand(new WaitImageCommandSettings
        {
            ImagePath = string.Empty
        })
        {
            LineNumber = 12
        };

        var ex = await Assert.ThrowsAsync<CommandValidationException>(() => command.Execute(CancellationToken.None));

        Assert.Equal(12, ex.LineNumber);
        Assert.Equal(CommandValidationErrorCodes.ImagePathRequired, ex.ErrorCode);
        Assert.Equal(nameof(IWaitImageCommandSettings.ImagePath), ex.PropertyName);
    }

    private sealed class TestPathResolver : IPathResolver
    {
        public TestPathResolver(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
        }

        public string BaseDirectory { get; }

        public string ToAbsolutePath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.Combine(BaseDirectory, relativePath);
        }

        public string ToRelativePath(string absolutePath) => absolutePath;
    }

    private sealed class TestValidationCommand : BaseCommand
    {
        public TestValidationCommand(ICommandSettings settings) : base(parent: null, settings) { }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
