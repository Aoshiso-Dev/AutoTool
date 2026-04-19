namespace AutoTool.Application.Ports;

public interface IFileSystemPathService
{
    bool FileExists(string filePath);
    string GetFileName(string filePath);
}
