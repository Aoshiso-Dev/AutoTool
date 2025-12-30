using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Services.Interfaces;

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

        private readonly FileTypeInfo _fileTypeInfo;
        private readonly Action<string> _saveFunc;
        private readonly Action<string> _loadFunc;
        private readonly IFileDialogService _fileDialogService;
        private readonly IRecentFileStore _recentFileStore;

        [ObservableProperty]
        private bool isFileOpened = false;

        [ObservableProperty]
        private string _currentFileName = string.Empty;

        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<RecentFile>? _recentFiles;


        public FileManager(
            FileTypeInfo fileTypeInfo,
            Action<string> saveFunc,
            Action<string> loadFunc,
            IFileDialogService fileDialogService,
            IRecentFileStore recentFileStore)
        {
            _fileTypeInfo = fileTypeInfo;
            _saveFunc = saveFunc;
            _loadFunc = loadFunc;
            _fileDialogService = fileDialogService;
            _recentFileStore = recentFileStore;

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
                filePath = _fileDialogService.OpenFile(CreateDialogOptions()) ?? string.Empty;
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }
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
            var filePath = _fileDialogService.SaveFile(CreateDialogOptions());
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            _saveFunc(filePath);

            AddToRecentFiles(filePath);

            CurrentFilePath = filePath;
            CurrentFileName = System.IO.Path.GetFileName(filePath);

            IsFileOpened = true;
        }

        private FileDialogOptions CreateDialogOptions()
        {
            return new FileDialogOptions(
                _fileTypeInfo.Title,
                _fileTypeInfo.Filter,
                _fileTypeInfo.FilterIndex,
                _fileTypeInfo.RestoreDirectory,
                _fileTypeInfo.DefaultExt);
        }

        private void LoadRecentFiles()
        {
            RecentFiles = _recentFileStore.Load(GetRecentFilesKey());

            if (RecentFiles == null)
            {
                RecentFiles = new ObservableCollection<RecentFile>();
            }
        }

        private void SaveRecentFiles()
        {
            _recentFileStore.Save(GetRecentFilesKey(), RecentFiles);
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

        private string GetRecentFilesKey() => $"RecentFiles_{_fileTypeInfo.DefaultExt}.xml";
    }
}
