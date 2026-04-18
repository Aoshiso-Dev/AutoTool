using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Panels.ViewModel.Shared;

namespace AutoTool.Panels.ViewModel;

public partial class LogPanelViewModel : ObservableObject, ILogPanelViewModel
{
    private readonly ObservableCollection<LogEntry> _logEntries = [];
    private readonly ICollectionView _filteredLogEntries;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<LogEntry> LogEntries => _logEntries;
    public ICollectionView FilteredLogEntries => _filteredLogEntries;

    public LogPanelViewModel(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _filteredLogEntries = CollectionViewSource.GetDefaultView(_logEntries);
        _filteredLogEntries.Filter = FilterLogEntry;
    }

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void Prepare()
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            _logEntries.Clear();
            return;
        }

        dispatcher.Invoke(() => _logEntries.Clear());
    }

    public void WriteLog(string text)
    {
        AppendLogEntry(new LogEntry
        {
            Timestamp = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss"),
            LineNumber = string.Empty,
            CommandName = "システム",
            Message = text,
            Level = LogEntryLevel.Info
        });
    }

    public void WriteLog(string lineNumber, string commandName, string detail)
    {
        AppendLogEntry(new LogEntry
        {
            Timestamp = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss"),
            LineNumber = lineNumber,
            CommandName = commandName,
            Message = detail,
            Level = DetectLevel(detail)
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        _filteredLogEntries.Refresh();
    }

    private bool FilterLogEntry(object obj)
    {
        if (obj is not LogEntry entry)
        {
            return false;
        }

        var keyword = SearchText?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return entry.SearchableText.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void AppendLogEntry(LogEntry entry)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            _logEntries.Add(entry);
            return;
        }

        dispatcher.Invoke(() => _logEntries.Add(entry));
    }

    private static LogEntryLevel DetectLevel(string detail)
    {
        return detail switch
        {
            _ when string.IsNullOrWhiteSpace(detail) => LogEntryLevel.Info,
            _ when ContainsAny(detail, "エラー", "error", "E_") => LogEntryLevel.Error,
            _ when ContainsAny(detail, "警告", "warning", "warn") => LogEntryLevel.Warning,
            _ when ContainsAny(detail, "開始", "start") => LogEntryLevel.Start,
            _ when ContainsAny(detail, "完了", "成功", "success") => LogEntryLevel.Success,
            _ => LogEntryLevel.Info
        };
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

