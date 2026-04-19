namespace AutoTool.Application.Ports;

public enum AppDialogTone
{
    Info,
    Warning,
    Error,
    Question
}

public sealed record AppDialogAction(string Id, string Label, bool IsDefault = false, bool IsCancel = false);

public interface IAppDialogService
{
    string? Show(
        string title,
        string message,
        IReadOnlyList<AppDialogAction> actions,
        AppDialogTone tone = AppDialogTone.Info,
        object? owner = null);
}
