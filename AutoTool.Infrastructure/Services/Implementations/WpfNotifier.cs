using AutoTool.Application.Ports;
using AutoTool.Commands.Services;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// ダイアログサービス経由で通知を表示する実装
/// </summary>
public class WpfNotifier : INotifier
{
    private readonly IAppDialogService _appDialogService;

    public WpfNotifier(IAppDialogService appDialogService)
    {
        ArgumentNullException.ThrowIfNull(appDialogService);
        _appDialogService = appDialogService;
    }

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
        _ = _appDialogService.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
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
