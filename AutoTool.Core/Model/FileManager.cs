using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using AutoTool.Core.Ports;

namespace AutoTool.Model;

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
        public int FilterIndex { get; set; }
        public bool RestoreDirectory { get; set; }
        public string DefaultExt { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    private readonly FileTypeInfo _fileTypeInfo;
    private readonly Action<string> _saveFunc;
    private readonly Action<string> _loadFunc;
    private readonly IFilePicker _filePicker;
    private readonly IRecentFileStore _recentFileStore;

    [ObservableProperty]
    private bool _isFileOpened;

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
        IFilePicker filePicker,
        IRecentFileStore recentFileStore)
    {
        _fileTypeInfo = fileTypeInfo ?? throw new ArgumentNullException(nameof(fileTypeInfo));
        _saveFunc = saveFunc ?? throw new ArgumentNullException(nameof(saveFunc));
        _loadFunc = loadFunc ?? throw new ArgumentNullException(nameof(loadFunc));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _recentFileStore = recentFileStore ?? throw new ArgumentNullException(nameof(recentFileStore));

        LoadRecentFiles();
    }

    public bool OpenFile(string filePath = "")
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = _filePicker.OpenFile(CreateDialogOptions()) ?? string.Empty;
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
        }

        if (!File.Exists(filePath))
        {
            RemoveFromRecentFiles(filePath);
            return false;
        }

        _loadFunc(filePath);
        AddToRecentFiles(filePath);

        CurrentFilePath = filePath;
        CurrentFileName = Path.GetFileName(filePath);
        IsFileOpened = true;
        return true;
    }

    public void SaveFile()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            _saveFunc(CurrentFilePath);
        }
    }

    public bool SaveFileAs()
    {
        var filePath = _filePicker.SaveFile(CreateDialogOptions());
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        _saveFunc(filePath);
        AddToRecentFiles(filePath);

        CurrentFilePath = filePath;
        CurrentFileName = Path.GetFileName(filePath);
        IsFileOpened = true;
        return true;
    }

    private FileDialogOptions CreateDialogOptions() => new(
        _fileTypeInfo.Title,
        _fileTypeInfo.Filter,
        _fileTypeInfo.FilterIndex,
        _fileTypeInfo.RestoreDirectory,
        _fileTypeInfo.DefaultExt);

    private void LoadRecentFiles()
    {
        RecentFiles = _recentFileStore.Load(GetRecentFilesKey()) ?? new ObservableCollection<RecentFile>();
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

        RecentFiles?.Insert(0, new RecentFile
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath
        });

        if (RecentFiles?.Count > 10)
        {
            RecentFiles?.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFiles();
    }

    private void RemoveFromRecentFiles(string filePath)
    {
        var existingItem = RecentFiles?.FirstOrDefault(f => f.FilePath == filePath);
        if (existingItem == null)
        {
            return;
        }

        RecentFiles?.Remove(existingItem);
        SaveRecentFiles();
    }

    private string GetRecentFilesKey() => $"RecentFiles_{_fileTypeInfo.DefaultExt}";
}

