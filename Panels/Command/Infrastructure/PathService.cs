using System.IO;
using System.Reflection;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Infrastructure;

/// <summary>
/// パス操作サービスの実装
/// </summary>
public class PathService : IPathService
{
    private readonly Lazy<string> _baseDirectory;

    public PathService()
    {
        _baseDirectory = new Lazy<string>(() => 
            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory);
    }

    public string BaseDirectory => _baseDirectory.Value;

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
}
