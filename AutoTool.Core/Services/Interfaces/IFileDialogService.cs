using System;

namespace AutoTool.Services.Interfaces
{
    public interface IFileDialogService
    {
        string? OpenFile(FileDialogOptions options);
        string? SaveFile(FileDialogOptions options);
    }

    public sealed record FileDialogOptions(
        string Title,
        string Filter,
        int FilterIndex,
        bool RestoreDirectory,
        string DefaultExt);
}
