using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;
using AutoTool.Application.Ports;

namespace AutoTool.Application.Files;

public partial class FileManager : ObservableObject
{
    public class FileTypeInfo
    {
        [SetsRequiredMembers]
        public FileTypeInfo()
        {
            Filter = string.Empty;
            DefaultExt = string.Empty;
            Title = string.Empty;
        }

        public required string Filter { get; set; }
        public int FilterIndex { get; set; }
        public bool RestoreDirectory { get; set; }
        public required string DefaultExt { get; set; }
        public required string Title { get; set; }
    }

    private readonly FileTypeInfo _fileTypeInfo;
    private readonly Action<string> _saveFunc;
    private readonly Action<string> _loadFunc;
    private readonly IFilePicker _filePicker;
    private readonly IRecentFileStore _recentFileStore;
    private readonly IFileSystemPathService _fileSystemPathService;

    [ObservableProperty]
    private bool _isFileOpened;

    [ObservableProperty]
    private string _currentFileName = string.Empty;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RecentFileEntry>? _recentFiles;

    public FileManager(
        FileTypeInfo fileTypeInfo,
        Action<string> saveFunc,
        Action<string> loadFunc,
        IFilePicker filePicker,
        IRecentFileStore recentFileStore,
        IFileSystemPathService fileSystemPathService)
    {
        ArgumentNullException.ThrowIfNull(fileTypeInfo);
        ArgumentNullException.ThrowIfNull(saveFunc);
        ArgumentNullException.ThrowIfNull(loadFunc);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(recentFileStore);
        ArgumentNullException.ThrowIfNull(fileSystemPathService);
        _fileTypeInfo = fileTypeInfo;
        _saveFunc = saveFunc;
        _loadFunc = loadFunc;
        _filePicker = filePicker;
        _recentFileStore = recentFileStore;
        _fileSystemPathService = fileSystemPathService;

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

        if (!_fileSystemPathService.FileExists(filePath))
        {
            RemoveFromRecentFiles(filePath);
            return false;
        }

        _loadFunc(filePath);
        AddToRecentFiles(filePath);

        CurrentFilePath = filePath;
        CurrentFileName = _fileSystemPathService.GetFileName(filePath);
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
        CurrentFileName = _fileSystemPathService.GetFileName(filePath);
        IsFileOpened = true;
        return true;
    }

    public void ResetToNewFile()
    {
        CurrentFilePath = string.Empty;
        CurrentFileName = string.Empty;
        IsFileOpened = false;
    }

    private FileDialogOptions CreateDialogOptions() => new(
        _fileTypeInfo.Title,
        _fileTypeInfo.Filter,
        _fileTypeInfo.FilterIndex,
        _fileTypeInfo.RestoreDirectory,
        _fileTypeInfo.DefaultExt);

    private void LoadRecentFiles()
    {
        RecentFiles = _recentFileStore.Load(GetRecentFilesKey()) ?? [];
    }

    private void SaveRecentFiles()
    {
        _recentFileStore.Save(GetRecentFilesKey(), RecentFiles);
    }

    private void AddToRecentFiles(string filePath)
    {
        var existingItem = RecentFiles?.FirstOrDefault(f =>
            string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existingItem is not null)
        {
            RecentFiles?.Remove(existingItem);
        }

        RecentFiles?.Insert(0, new RecentFileEntry
        {
            FileName = _fileSystemPathService.GetFileName(filePath),
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
        var existingItem = RecentFiles?.FirstOrDefault(f =>
            string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existingItem is null)
        {
            return;
        }

        RecentFiles?.Remove(existingItem);
        SaveRecentFiles();
    }

    private string GetRecentFilesKey() => $"RecentFiles_{_fileTypeInfo.DefaultExt}";
}
