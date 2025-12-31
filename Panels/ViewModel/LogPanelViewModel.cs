using CommunityToolkit.Mvvm.ComponentModel;

namespace MacroPanels.ViewModel;

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
        Log += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}";
    }
    
    public void WriteLog(string lineNumber, string commandName, string detail)
    {
        Log += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {lineNumber.PadRight(20)} {commandName.PadRight(20)} {detail}{Environment.NewLine}";
    }
}