namespace AutoTool.Application.Ports;

/// <summary>
/// アプリ内ダイアログの表示トーンを表します。
/// </summary>
public enum AppDialogTone
{
    Info,
    Warning,
    Error,
    Question
}

/// <summary>
/// ダイアログ上の選択アクション定義です。
/// </summary>
public sealed record AppDialogAction(
    string Id,
    string Label,
    bool IsDefault = false,
    bool IsCancel = false,
    bool CloseDialogOnClick = true);

/// <summary>
/// アプリ汎用ダイアログを表示するポートです。
/// </summary>
public interface IAppDialogService
{
    /// <summary>
    /// ダイアログを表示し、押下されたアクション ID を返します。
    /// </summary>
    string? Show(
        string title,
        string message,
        IReadOnlyList<AppDialogAction> actions,
        AppDialogTone tone = AppDialogTone.Info,
        object? owner = null);
}
