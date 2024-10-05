using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.Model
{
    public partial class FileManager : ObservableObject
    {
        public class RecentFile
        {
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }

        public class FileTypeInfo
        {
            public string Filter { get; set; } = string.Empty;
            public int FilterIndex { get; set; } = 0;
            public bool RestoreDirectory { get; set; }
            public string DefaultExt { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
        }

        private FileTypeInfo _fileTypeInfo;
        private Action<string> _saveFunc;
        private Action<string> _loadFunc;

        [ObservableProperty]
        private bool isFileOpened = false;

        [ObservableProperty]
        private string _currentFileName = string.Empty;

        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<RecentFile>? _recentFiles;


        public FileManager(FileTypeInfo fileTypeInfo, Action<string> saveFunc, Action<string>loadFunc)
        {
            _fileTypeInfo = fileTypeInfo;
            _saveFunc = saveFunc;
            _loadFunc = loadFunc;

            LoadRecentFiles();
        }

        ~FileManager()
        {
            SaveRecentFiles();
        }

        public void OpenFile(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var dialog = new OpenFileDialog()
                {
                    Title = _fileTypeInfo.Title,
                    Filter = _fileTypeInfo.Filter,
                    FilterIndex = _fileTypeInfo.FilterIndex,
                    RestoreDirectory = _fileTypeInfo.RestoreDirectory,
                    DefaultExt = _fileTypeInfo.DefaultExt,
                };

                dialog.ShowDialog();

                if (dialog.FileName == "")
                {
                    return;
                }

                filePath = dialog.FileName;
            }

            _loadFunc(filePath);

            AddToRecentFiles(filePath);

            CurrentFilePath = filePath;
            CurrentFileName = System.IO.Path.GetFileName(filePath);

            IsFileOpened = true;
        }

        public void SaveFile()
        {
            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                _saveFunc(CurrentFilePath);
            }
        }

        public void SaveFileAs()
        {
            var dialog = new SaveFileDialog()
            {
                Title = _fileTypeInfo.Title,
                Filter = _fileTypeInfo.Filter,
                FilterIndex = _fileTypeInfo.FilterIndex,
                RestoreDirectory = _fileTypeInfo.RestoreDirectory,
                DefaultExt = _fileTypeInfo.DefaultExt,
            };

            dialog.ShowDialog();

            if (dialog.FileName == "")
            {
                return;
            }

            _saveFunc(dialog.FileName);

            AddToRecentFiles(dialog.FileName);

            CurrentFilePath = dialog.FileName;
            CurrentFileName = System.IO.Path.GetFileName(dialog.FileName);

            IsFileOpened = true;
        }



        private void LoadRecentFiles()
        {
            RecentFiles = XmlSerializer.XmlSerializer.DeserializeFromFile<ObservableCollection<RecentFile>>($"RecentFiles_{_fileTypeInfo.DefaultExt}.xml");

            if (RecentFiles == null)
            {
                RecentFiles = new ObservableCollection<RecentFile>();
            }
        }

        private void SaveRecentFiles()
        {
            XmlSerializer.XmlSerializer.SerializeToFile(RecentFiles, $"RecentFiles_{_fileTypeInfo.DefaultExt}.xml");
        }

        private void AddToRecentFiles(string filePath)
        {
            var existingItem = RecentFiles?.FirstOrDefault(f => f.FilePath == filePath);
            if (existingItem != null)
            {
                RecentFiles?.Remove(existingItem);
            }

            RecentFiles?.Insert(0, new RecentFile { FileName = System.IO.Path.GetFileName(filePath), FilePath = filePath });

            if (RecentFiles?.Count > 10)
            {
                RecentFiles?.RemoveAt(RecentFiles.Count - 1);
            }

            SaveRecentFiles();
        }
    }
}
