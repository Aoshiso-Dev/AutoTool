using AutoTool.Services.Interfaces;
using Microsoft.Win32;

namespace AutoTool.Services.Implementations
{
    public class WpfFileDialogService : IFileDialogService
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
}
