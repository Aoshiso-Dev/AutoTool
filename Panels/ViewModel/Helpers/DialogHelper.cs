using System;
using System.IO;
using Microsoft.Win32;
using MacroPanels.Helpers;

namespace MacroPanels.ViewModel.Helpers
{
    /// <summary>
    /// ファイル・フォルダ選択ダイアログのヘルパー
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// 画像ファイル選択ダイアログ
        /// </summary>
        public static string? SelectImageFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// ONNXモデルファイル選択ダイアログ
        /// </summary>
        public static string? SelectModelFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ONNX Files (*.onnx)|*.onnx|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// 実行ファイル選択ダイアログ
        /// </summary>
        public static string? SelectExecutableFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// フォルダ選択ダイアログ
        /// </summary>
        public static string? SelectFolder()
        {
            var dialog = new OpenFolderDialog { Multiselect = false };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        /// <summary>
        /// キャプチャ保存パスの生成（相対パス対応）
        /// </summary>
        public static string CreateCaptureFilePath()
        {
            // AutoTool.exeからの相対パスでCaptureフォルダを作成
            var appDirectory = PathHelper.GetApplicationDirectory();
            var captureDirectory = Path.Combine(appDirectory, "Capture");
            
            if (!Directory.Exists(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                System.Diagnostics.Debug.WriteLine($"キャプチャディレクトリを作成: {captureDirectory}");
            }

            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
            var fullPath = Path.Combine(captureDirectory, fileName);
            
            System.Diagnostics.Debug.WriteLine($"キャプチャファイルパス生成: {fullPath}");
            return fullPath;
        }
    }
}