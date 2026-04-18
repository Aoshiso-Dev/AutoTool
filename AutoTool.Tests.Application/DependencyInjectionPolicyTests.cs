using System.IO;
using System.Text.RegularExpressions;

namespace AutoTool.Automation.Runtime.Tests;

public class DependencyInjectionPolicyTests
{
    private static readonly Regex[] ForbiddenPatterns =
    [
        new(@"\bIServiceProvider\b", RegexOptions.CultureInvariant),
        new(@"\bGetService\s*\(", RegexOptions.CultureInvariant),
        new(@"\bGetRequiredService\s*\(", RegexOptions.CultureInvariant)
    ];

    private static readonly string[] TargetDirectories =
    [
        "AutoTool.Application",
        "AutoTool.Automation.Contracts",
        "AutoTool.Automation.Runtime",
        "AutoTool.Bootstrap",
        "AutoTool.Desktop",
        "AutoTool.Domain",
        "AutoTool.Infrastructure"
    ];

    [Fact]
    public void ProductionCode_DoesNotUseServiceLocatorPatterns()
    {
        var root = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var directory in TargetDirectories)
        {
            var directoryPath = Path.Combine(root, directory);
            if (!Directory.Exists(directoryPath))
            {
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories))
            {
                if (IsGeneratedOrBuildArtifact(filePath))
                {
                    continue;
                }

                var content = File.ReadAllText(filePath);
                foreach (var pattern in ForbiddenPatterns)
                {
                    if (!pattern.IsMatch(content))
                    {
                        continue;
                    }

                    violations.Add($"{Path.GetRelativePath(root, filePath)}: {pattern}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Service Locator pattern が検出されました:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static bool IsGeneratedOrBuildArtifact(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AutoTool.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("AutoTool.sln が見つからないためリポジトリルートを特定できません。");
    }
}
