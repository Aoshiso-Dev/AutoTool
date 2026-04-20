using AutoTool.Application.Ports;
using Microsoft.Win32;

namespace AutoTool.Desktop.Services;

public class WpfFilePicker : IFilePicker
{
    public string? OpenFile(FileDialogOptions options)
    {
        var dialog = new OpenFileDialog
        {
            Title = options.Title,
            Filter = options.Filter,
            FilterIndex = options.FilterIndex,
            RestoreDirectory = options.RestoreDirectory,
            DefaultExt = options.DefaultExt,
        };

        var result = dialog.ShowDialog();
        return result == true && !string.IsNullOrEmpty(dialog.FileName) ? dialog.FileName : null;
    }

    public string? SaveFile(FileDialogOptions options)
    {
        var dialog = new SaveFileDialog
        {
            Title = options.Title,
            Filter = options.Filter,
            FilterIndex = options.FilterIndex,
            RestoreDirectory = options.RestoreDirectory,
            DefaultExt = options.DefaultExt,
        };

        var result = dialog.ShowDialog();
        return result == true && !string.IsNullOrEmpty(dialog.FileName) ? dialog.FileName : null;
    }
}
