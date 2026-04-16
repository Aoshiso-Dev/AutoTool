using System;

namespace AutoTool.Core.Ports;

public interface IFilePicker
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

