using System;
using System.IO;
using Microsoft.Win32;

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
        /// キャプチャ保存パスの生成
        /// </summary>
        public static string CreateCaptureFilePath()
        {
            var captureDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Capture");
            if (!Directory.Exists(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
            }

            return Path.Combine(captureDirectory, $"{DateTime.Now:yyyyMMddHHmmss}.png");
        }
    }
}