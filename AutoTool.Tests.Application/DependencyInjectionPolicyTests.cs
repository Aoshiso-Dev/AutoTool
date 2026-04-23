using System.IO;
using System.Text.RegularExpressions;

namespace AutoTool.Automation.Runtime.Tests;

/// <summary>
/// DI 方針と依存ルール違反がないことを検証するテストです。
/// </summary>
public class DependencyInjectionPolicyTests
{
    private static readonly Regex[] ForbiddenPatterns =
    [
        new(@"\bIServiceProvider\b", RegexOptions.CultureInvariant),
        new(@"\bGetService\s*\(", RegexOptions.CultureInvariant),
        new(@"\bGetRequiredService\s*\(", RegexOptions.CultureInvariant)
    ];

    private static readonly string[] LegacyNamespaceMarkers =
    [
        "AutoTool.Core",
        "AutoTool.Commands.Abstractions",
        "AutoTool.Panels",
        "AutoTool.ViewModel",
        "AutoTool.View.",
        "AutoTool.Hosting"
    ];

    private static readonly string[] TargetDirectories =
    [
        "AutoTool.Application",
        "AutoTool.Automation.Contracts",
        "AutoTool.Automation.Runtime",
        "AutoTool.Bootstrap",
        "AutoTool.Desktop",
        "AutoTool.Domain",
        "AutoTool.Infrastructure",
        "AutoTool.Plugin.Abstractions",
        "AutoTool.Plugin.Host"
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

            foreach (var filePath in EnumerateProductionFiles(directoryPath, "*.cs"))
            {
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

    [Fact]
    public void ProductionCode_DoesNotReferenceLegacyNamespaces()
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

            foreach (var filePath in EnumerateProductionFiles(directoryPath, "*.cs")
                .Concat(EnumerateProductionFiles(directoryPath, "*.xaml")))
            {
                var content = File.ReadAllText(filePath);
                foreach (var marker in LegacyNamespaceMarkers)
                {
                    if (!content.Contains(marker, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    violations.Add($"{Path.GetRelativePath(root, filePath)}: {marker}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "旧 namespace 参照が検出されました:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateProductionFiles(string directoryPath, string searchPattern)
    {
        return Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.AllDirectories)
            .Where(filePath => !IsGeneratedOrBuildArtifact(filePath));
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


