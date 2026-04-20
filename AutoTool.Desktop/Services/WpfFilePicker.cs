using AutoTool.Application.Ports;
using Microsoft.Win32;

namespace AutoTool.Desktop.Services;

/// <summary>
/// WPF のファイルダイアログを用いてファイル選択を行う実装です。
/// </summary>
public class WpfFilePicker : IFilePicker
{
    /// <summary>
    /// ファイルを開くダイアログを表示し、選択結果を返します。
    /// </summary>
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

    /// <summary>
    /// ファイル保存ダイアログを表示し、保存先パスを返します。
    /// </summary>
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
