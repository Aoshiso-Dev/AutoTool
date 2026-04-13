using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Panels.ViewModel;

public partial class LogPanelViewModel : ObservableObject, ILogPanelViewModel
{
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _log = string.Empty;

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void Prepare() => Log = string.Empty;

    public void WriteLog(string text)
    {
        AppendLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}");
    }
    
    public void WriteLog(string lineNumber, string commandName, string detail)
    {
        AppendLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {lineNumber.PadRight(20)} {commandName.PadRight(20)} {detail}{Environment.NewLine}");
    }

    private void AppendLog(string message)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            Log += message;
            return;
        }

        dispatcher.Invoke(() => Log += message);
    }
}
