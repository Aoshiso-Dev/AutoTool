using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;
using AutoTool.Application.Ports;

namespace AutoTool.Application.Files;

/// <summary>
/// 現在開いているマクロファイルの場所を共有します。
/// </summary>
public interface ICurrentMacroFileContext
{
    string? CurrentMacroFilePath { get; set; }
    string? BaseDirectory { get; }
}

/// <summary>
/// 現在開いているマクロファイルの場所を保持する標準実装です。
/// </summary>
public sealed class CurrentMacroFileContext : ICurrentMacroFileContext
{
    public string? CurrentMacroFilePath { get; set; }

    public string? BaseDirectory
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CurrentMacroFilePath))
            {
                return null;
            }

            try
            {
                return Path.GetDirectoryName(Path.GetFullPath(CurrentMacroFilePath));
            }
            catch
            {
                return null;
            }
        }
    }
}

/// <summary>
/// マクロファイルの読み込み・保存と最近使ったファイル一覧の更新を一元管理します。
/// </summary>
public partial class FileManager : ObservableObject
{
    /// <summary>
    /// ダイアログ表示に必要なファイル種別情報です。
    /// </summary>
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
    private readonly ICurrentMacroFileContext? _currentMacroFileContext;

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
        IFileSystemPathService fileSystemPathService,
        ICurrentMacroFileContext? currentMacroFileContext = null)
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
        _currentMacroFileContext = currentMacroFileContext;

        LoadRecentFiles();
    }

    /// <summary>
    /// ファイルを開いて読み込み、現在ファイル情報と最近使ったファイルを更新します。
    /// </summary>
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
        _currentMacroFileContext?.CurrentMacroFilePath = CurrentFilePath;
        return true;
    }

    /// <summary>
    /// 現在開いているファイルパスへ上書き保存します。
    /// </summary>
    public void SaveFile()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            _saveFunc(CurrentFilePath);
        }
    }

    /// <summary>
    /// 保存先を選択して保存し、現在ファイル情報と最近使ったファイルを更新します。
    /// </summary>
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
        _currentMacroFileContext?.CurrentMacroFilePath = CurrentFilePath;
        return true;
    }

    /// <summary>
    /// 現在ファイル状態を未保存の新規状態へ戻します。
    /// </summary>
    public void ResetToNewFile()
    {
        CurrentFilePath = string.Empty;
        CurrentFileName = string.Empty;
        IsFileOpened = false;
        _currentMacroFileContext?.CurrentMacroFilePath = null;
    }

    /// <summary>
    /// ファイルダイアログ設定を生成します。
    /// </summary>
    private FileDialogOptions CreateDialogOptions() => new(
        _fileTypeInfo.Title,
        _fileTypeInfo.Filter,
        _fileTypeInfo.FilterIndex,
        _fileTypeInfo.RestoreDirectory,
        _fileTypeInfo.DefaultExt);

    /// <summary>
    /// 永続ストアから最近使ったファイル一覧を読み込みます。
    /// </summary>
    private void LoadRecentFiles()
    {
        RecentFiles = _recentFileStore.Load(GetRecentFilesKey()) ?? [];
    }

    /// <summary>
    /// 最近使ったファイル一覧を永続化します。
    /// </summary>
    private void SaveRecentFiles()
    {
        _recentFileStore.Save(GetRecentFilesKey(), RecentFiles);
    }

    /// <summary>
    /// 最近使ったファイル一覧の先頭へ追加し、重複除去と件数上限を適用します。
    /// </summary>
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

    /// <summary>
    /// 指定パスを最近使ったファイル一覧から削除します。
    /// </summary>
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

    /// <summary>
    /// このファイル種別専用の最近使ったファイル保存キーを返します。
    /// </summary>
    private string GetRecentFilesKey() => $"RecentFiles_{_fileTypeInfo.DefaultExt}";
}
