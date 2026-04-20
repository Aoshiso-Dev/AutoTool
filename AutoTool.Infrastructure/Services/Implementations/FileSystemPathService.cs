using AutoTool.Application.Ports;
using System.IO;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// 関連機能の共通処理を提供し、呼び出し側の重複実装を減らします。
/// </summary>
public sealed class FileSystemPathService : IFileSystemPathService
{
    public bool FileExists(string filePath) => File.Exists(filePath);

    public string GetFileName(string filePath) => Path.GetFileName(filePath);
}
