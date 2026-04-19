using AutoTool.Commands.Services;
using AutoTool.Infrastructure.Ui;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// WPFを使用した通知の実装
/// </summary>
public class WpfNotifier : INotifier
{
    public void ShowInfo(string message, string title)
    {
        _ = AutoToolDialog.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
            AutoToolDialogTone.Info);
    }

    public void ShowWarning(string message, string title)
    {
        _ = AutoToolDialog.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
            AutoToolDialogTone.Warning);
    }

    public void ShowError(string message, string title)
    {
        _ = AutoToolDialog.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true, IsCancel: true)],
            AutoToolDialogTone.Error);
    }

    public bool ShowConfirm(string message, string title)
    {
        var result = AutoToolDialog.Show(
            title,
            message,
            [new("ok", "OK", IsDefault: true), new("cancel", "キャンセル", IsCancel: true)],
            AutoToolDialogTone.Question);

        return string.Equals(result, "ok", StringComparison.Ordinal);
    }
}
