using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Desktop.Panels.ViewModel.Shared;

namespace AutoTool.Desktop.Panels.ViewModel;

public partial class LogPanelViewModel : ObservableObject, ILogPanelViewModel
{
    private readonly ObservableCollection<LogEntry> _logEntries = [];
    private readonly ICollectionView _filteredLogEntries;
    private readonly TimeProvider _timeProvider;
    public event Action<string>? StatusMessageRequested;

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

    [RelayCommand]
    private void CopyLogs()
    {
        var entries = _filteredLogEntries.Cast<LogEntry>().ToList();
        if (entries.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder(entries.Count * 48);
        foreach (var entry in entries)
        {
            builder.Append(entry.Timestamp)
                .Append('\t')
                .Append(entry.LineNumber)
                .Append('\t')
                .Append(entry.CommandName)
                .Append('\t')
                .Append(entry.Message)
                .AppendLine();
        }

        var dispatcher = System.Windows.Application.Current.Dispatcher;
        try
        {
            if (dispatcher.CheckAccess())
            {
                System.Windows.Clipboard.SetText(builder.ToString());
            }
            else
            {
                dispatcher.Invoke(() => System.Windows.Clipboard.SetText(builder.ToString()));
            }
        }
        catch (Exception ex)
        {
            WriteLog(string.Empty, "システム", $"警告: ログをクリップボードへコピーできませんでした。{ex.Message}");
            StatusMessageRequested?.Invoke("ログのコピーに失敗しました。");
            return;
        }

        StatusMessageRequested?.Invoke($"ログをコピーしました（{entries.Count}件）。");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        var count = _logEntries.Count;
        Prepare();
        StatusMessageRequested?.Invoke($"ログをクリアしました（{count}件）。");
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

