using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 最近使用したファイルサービスの実装
/// </summary>
public class RecentFileService : IRecentFileService
{
    private readonly ILogger<RecentFileService> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly List<string> _recentFiles = new();
    private const string RecentFilesKey = "RecentFiles";
    private const string MaxRecentFilesKey = "MaxRecentFiles";

    public RecentFileService(ILogger<RecentFileService> logger, IConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
        LoadRecentFiles();
    }

    public int MaxRecentFiles
    {
        get => _configurationService.Get<int>(MaxRecentFilesKey, 10);
        set => _configurationService.Set(MaxRecentFilesKey, value);
    }

    public IEnumerable<string> RecentFiles => _recentFiles.AsReadOnly();

    public IEnumerable<string> GetRecentFiles()
    {
        return _recentFiles.AsReadOnly();
    }

    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        
        // 既存のエントリを削除
        _recentFiles.Remove(normalizedPath);
        
        // 先頭に追加
        _recentFiles.Insert(0, normalizedPath);
        
        // 最大数を超えた場合は末尾から削除
        while (_recentFiles.Count > MaxRecentFiles)
        {
            _recentFiles.RemoveAt(_recentFiles.Count - 1);
        }
        
        SaveRecentFiles();
        _logger.LogDebug("Added recent file: {FilePath}", normalizedPath);
    }

    public void RemoveRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        if (_recentFiles.Remove(normalizedPath))
        {
            SaveRecentFiles();
            _logger.LogDebug("Removed recent file: {FilePath}", normalizedPath);
        }
    }

    public void ClearRecentFiles()
    {
        _recentFiles.Clear();
        SaveRecentFiles();
        _logger.LogDebug("Cleared all recent files");
    }

    public void SaveRecentFiles()
    {
        try
        {
            _configurationService.Set(RecentFilesKey, _recentFiles.ToArray());
            _ = _configurationService.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recent files");
        }
    }

    private void LoadRecentFiles()
    {
        try
        {
            var recentFiles = _configurationService.Get<string[]>(RecentFilesKey, Array.Empty<string>());
            _recentFiles.Clear();
            
            if (recentFiles != null)
            {
                // 存在するファイルのみを追加
                foreach (var filePath in recentFiles)
                {
                    if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                    {
                        _recentFiles.Add(filePath);
                    }
                }
            }
            
            _logger.LogDebug("Loaded {Count} recent files", _recentFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent files");
        }
    }
}