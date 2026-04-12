using AutoTool.Core.Ports;
using Microsoft.Win32;

namespace AutoTool.Infrastructure.Implementations;

public sealed class WpfPanelDialogService : IPanelDialogService
{
    public string? SelectImageFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectModelFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ONNX Files (*.onnx)|*.onnx|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectExecutableFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectFolder()
    {
        var dialog = new OpenFolderDialog { Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
