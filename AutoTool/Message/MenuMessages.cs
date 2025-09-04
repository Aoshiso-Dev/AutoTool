namespace AutoTool.Message
{
    /// <summary>
    /// プラグイン読み込みメッセージ
    /// </summary>
    public record LoadPluginMessage(string FilePath);

    /// <summary>
    /// プラグイン再読み込みメッセージ
    /// </summary>
    public record RefreshPluginsMessage;

    /// <summary>
    /// プラグイン情報表示メッセージ
    /// </summary>
    public record ShowPluginInfoMessage;

    /// <summary>
    /// パフォーマンス情報更新メッセージ
    /// </summary>
    public record RefreshPerformanceMessage;

    /// <summary>
    /// ログクリアメッセージ
    /// </summary>
    public record ClearLogMessage;
}