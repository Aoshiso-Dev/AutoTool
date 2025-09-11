namespace AutoTool.Services.Abstractions;

/// <summary>
/// UI関連サービスのインターフェース
/// </summary>
public interface IUIService
{
    /// <summary>
    /// メッセージボックスを表示します
    /// </summary>
    Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
    
    /// <summary>
    /// ファイル選択ダイアログを表示します
    /// </summary>
    Task<string?> ShowOpenFileDialogAsync(string filter = "", string initialDirectory = "");
    
    /// <summary>
    /// ファイル保存ダイアログを表示します
    /// </summary>
    Task<string?> ShowSaveFileDialogAsync(string filter = "", string initialDirectory = "", string defaultFileName = "");
    
    /// <summary>
    /// フォルダ選択ダイアログを表示します
    /// </summary>
    Task<string?> ShowFolderBrowserDialogAsync(string description = "");
    
    /// <summary>
    /// 進捗ダイアログを表示します
    /// </summary>
    IProgressDialog ShowProgressDialog(string title, string message, bool cancellable = false);
    
    /// <summary>
    /// トースト通知を表示します
    /// </summary>
    void ShowToast(string message, ToastType type = ToastType.Information);
}

/// <summary>
/// メッセージボックスの結果
/// </summary>
public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

/// <summary>
/// メッセージボックスのボタン
/// </summary>
public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

/// <summary>
/// メッセージボックスのアイコン
/// </summary>
public enum MessageBoxImage
{
    None,
    Information,
    Warning,
    Error,
    Question
}

/// <summary>
/// トーストの種類
/// </summary>
public enum ToastType
{
    Information,
    Warning,
    Error,
    Success
}

/// <summary>
/// 進捗ダイアログのインターフェース
/// </summary>
public interface IProgressDialog : IDisposable
{
    /// <summary>
    /// 進捗を更新します
    /// </summary>
    void UpdateProgress(int percentage, string? message = null);
    
    /// <summary>
    /// ダイアログを閉じます
    /// </summary>
    void Close();
    
    /// <summary>
    /// キャンセルされたかどうかを取得します
    /// </summary>
    bool IsCancelled { get; }
}