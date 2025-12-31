namespace MacroPanels.Command.Services;

/// <summary>
/// 通知サービスのインターフェース
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 情報メッセージを表示します
    /// </summary>
    void ShowInfo(string message, string title);

    /// <summary>
    /// 警告メッセージを表示します
    /// </summary>
    void ShowWarning(string message, string title);

    /// <summary>
    /// エラーメッセージを表示します
    /// </summary>
    void ShowError(string message, string title);

    /// <summary>
    /// 確認ダイアログを表示します
    /// </summary>
    /// <returns>ユーザーがOKを選択した場合はtrue</returns>
    bool ShowConfirm(string message, string title);
}
