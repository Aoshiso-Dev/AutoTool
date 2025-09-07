// AutoTool メッセージング システム（統一版）
namespace AutoTool.Message
{
    // VM間直接参照回避用の内部メッセージ
    
    /// <summary>
    /// 内部ファイル読み込みメッセージ（MainWindowViewModel -> ListPanelViewModel）
    /// </summary>
    public record InternalLoadMessage(string? FilePath = null);

    /// <summary>
    /// 内部ファイル保存メッセージ（MainWindowViewModel -> ListPanelViewModel）
    /// </summary>
    public record InternalSaveMessage(string? FilePath = null);

    /// <summary>
    /// 内部マクロ実行メッセージ（MainWindowViewModel -> ListPanelViewModel）
    /// </summary>
    public record InternalRunMessage();

    /// <summary>
    /// 内部マクロ停止メッセージ（MainWindowViewModel -> ListPanelViewModel）
    /// </summary>
    public record InternalStopMessage();

    /// <summary>
    /// コマンド数変更通知メッセージ（ListPanelViewModel -> MainWindowViewModel）
    /// </summary>
    public record CommandCountChangedMessage(int Count);
}