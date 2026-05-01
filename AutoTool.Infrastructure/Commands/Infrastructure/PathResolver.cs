using System.IO;
using System.Reflection;
using AutoTool.Application.Files;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// パス解決の実装
/// </summary>
public class PathResolver : IPathResolver
{
    private readonly Lazy<string> _applicationBaseDirectory;
    private readonly ICurrentMacroFileContext _currentMacroFileContext;

    public PathResolver()
        : this(new CurrentMacroFileContext())
    {
    }

    public PathResolver(ICurrentMacroFileContext currentMacroFileContext)
        : this(currentMacroFileContext, null)
    {
    }

    protected PathResolver(ICurrentMacroFileContext currentMacroFileContext, string? applicationBaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(currentMacroFileContext);

        _currentMacroFileContext = currentMacroFileContext;
        _applicationBaseDirectory = new Lazy<string>(() =>
            applicationBaseDirectory
            ?? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
            ?? Environment.CurrentDirectory);
    }

    public string BaseDirectory => ResolveBaseDirectory();

    public string ToAbsolutePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return relativePath;

        try
        {
            // 既に絶対パスの場合はそのまま返す
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            var absolutePath = Path.Combine(BaseDirectory, relativePath);
            return Path.GetFullPath(absolutePath);
        }
        catch
        {
            return relativePath;
        }
    }

    public string ToRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return absolutePath;

        try
        {
            var uri1 = new Uri(BaseDirectory + Path.DirectorySeparatorChar);
            var uri2 = new Uri(absolutePath);

            if (uri1.Scheme != uri2.Scheme)
            {
                return absolutePath;
            }

            var relativeUri = uri1.MakeRelativeUri(uri2);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return absolutePath;
        }
    }

    private string ResolveBaseDirectory()
    {
        var macroBaseDirectory = _currentMacroFileContext.BaseDirectory;
        return string.IsNullOrWhiteSpace(macroBaseDirectory)
            ? _applicationBaseDirectory.Value
            : macroBaseDirectory;
    }
}

