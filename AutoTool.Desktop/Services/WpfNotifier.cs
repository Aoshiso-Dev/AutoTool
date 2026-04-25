using AutoTool.Application.Ports;
using INotifier = AutoTool.Commands.Services.INotifier;

namespace AutoTool.Desktop.Services;

/// <summary>
/// AutoTool 共通ダイアログでユーザー通知を表示します。
/// </summary>
public class WpfNotifier(IAppDialogService appDialogService, ILogWriter logWriter) : INotifier
{
    private readonly IAppDialogService _appDialogService = appDialogService ?? throw new ArgumentNullException(nameof(appDialogService));
    private readonly ILogWriter _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));

    public void ShowInfo(string message, string title)
    {
        _ = _appDialogService.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
            AppDialogTone.Info);
    }

    public void ShowWarning(string message, string title)
    {
        _ = _appDialogService.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
            AppDialogTone.Warning);
    }

    public void ShowError(string message, string title)
    {
        _logWriter.WriteStructured(
            "Notifier",
            "ShowError",
            new Dictionary<string, object?>
            {
                ["Title"] = title,
                ["Message"] = message
            });

        _ = _appDialogService.Show(
            title,
            message,
            [
                new("copy", "コピー", CloseDialogOnClick: false),
                new("ok", "OK", IsDefault: true, IsCancel: true)
            ],
            AppDialogTone.Error);
    }

    public bool ShowConfirm(string message, string title)
    {
        var result = _appDialogService.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true), new("cancel", "キャンセル", IsCancel: true)],
            AppDialogTone.Question);

        return string.Equals(result, "ok", StringComparison.Ordinal);
    }
}
