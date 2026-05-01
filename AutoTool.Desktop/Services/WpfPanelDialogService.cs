using AutoTool.Application.Ports;
using Microsoft.Win32;

namespace AutoTool.Desktop.Services;

/// <summary>
/// 関連機能の共通処理を提供し、呼び出し側の重複実装を減らします。
/// </summary>
public sealed class WpfPanelDialogService : IPanelDialogService
{
    private const string AllFilesFilter = "All Files (*.*)|*.*";

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

    public string? SelectLabelFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Label Files (*.txt;*.names;*.yaml;*.yml)|*.txt;*.names;*.yaml;*.yml|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectFile(string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = string.IsNullOrWhiteSpace(filter) ? AllFilesFilter : filter,
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
