using System;
using System.IO;
using Microsoft.Win32;
using MacroPanels.Helpers;

namespace MacroPanels.ViewModel.Helpers
{
    /// <summary>
    /// �t�@�C���E�t�H���_�I���_�C�A���O�̃w���p�[
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// �摜�t�@�C���I���_�C�A���O
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
        /// ONNX���f���t�@�C���I���_�C�A���O
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
        /// ���s�t�@�C���I���_�C�A���O
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
        /// �t�H���_�I���_�C�A���O
        /// </summary>
        public static string? SelectFolder()
        {
            var dialog = new OpenFolderDialog { Multiselect = false };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        /// <summary>
        /// �L���v�`���ۑ��p�X�̐����i���΃p�X�Ή��j
        /// </summary>
        public static string CreateCaptureFilePath()
        {
            // AutoTool.exe����̑��΃p�X��Capture�t�H���_���쐬
            var appDirectory = PathHelper.GetApplicationDirectory();
            var captureDirectory = Path.Combine(appDirectory, "Capture");
            
            if (!Directory.Exists(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                System.Diagnostics.Debug.WriteLine($"�L���v�`���f�B���N�g�����쐬: {captureDirectory}");
            }

            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.png";
            var fullPath = Path.Combine(captureDirectory, fileName);
            
            System.Diagnostics.Debug.WriteLine($"�L���v�`���t�@�C���p�X����: {fullPath}");
            return fullPath;
        }
    }
}