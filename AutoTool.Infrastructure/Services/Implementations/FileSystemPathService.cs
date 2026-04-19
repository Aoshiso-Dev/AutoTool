using AutoTool.Application.Ports;
using System.IO;

namespace AutoTool.Infrastructure.Implementations;

public sealed class FileSystemPathService : IFileSystemPathService
{
    public bool FileExists(string filePath) => File.Exists(filePath);

    public string GetFileName(string filePath) => Path.GetFileName(filePath);
}
